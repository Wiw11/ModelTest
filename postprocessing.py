# -*- coding: UTF-8 -*-
'''
模型运行输出结果后处理
@Author：Cao Duanxiang
@Date：2026/01/07
'''
import os
import sys
import re
import struct
from tqdm import tqdm
import pandas as pd
import geopandas as gpd
from shapely.geometry import Polygon
from convertion import *


class PostProcessing():
    def __init__(self, input_dir, output_dir,multiple):
        self.input_dir = input_dir
        self.output_dir = output_dir
        self.multiple = multiple

    def _read_cell_info(self):
        # 读取网格信息
        cell_area_csv_path = self.output_dir + '\\cell_area.csv'
        if os.path.exists(cell_area_csv_path):     # 如果已存在网格面积信息，则直接读取
            cell_sr = pd.read_csv(cell_area_csv_path, index_col=0).iloc[:, 0]
        else:
            cell_path = os.path.dirname(self.input_dir) + '\\Template\\Cell.txt'
            with open(cell_path, 'r',encoding='utf-8') as file:
                lines = file.readlines()
            
            cell_df = pd.DataFrame(columns=['id', 'area'])
            for line in tqdm(lines[2:], desc='网格信息读取中...', unit='个'):
                text_ls = line.strip().split(" ")
                cell_df = pd.concat([cell_df, pd.DataFrame({'id': [int(text_ls[0])], 'area': [float(text_ls[5])]})], ignore_index=True)
            cell_sr = cell_df['area']
            cell_sr.index = cell_df['id'].values

            cell_sr.to_csv(cell_area_csv_path, index=True)   # 输出网格面积信息，供后续计算淹没面积使用

        return cell_sr

    def GuangdongHydrodynamic(self):
        # 读取网格信息
        cell_sr = self._read_cell_info()

        # 计算淹没面积
        result_txt = self.input_dir + '\\CellResult.txt'
        with open(result_txt, 'r',encoding='utf-8') as file:
            all_text = file.read()

        pattern = "\d{4}-\d{2}-\d{2}\s\d{2}:\d{2}:\d{2}\n"
        new_text = re.sub(pattern, '#', all_text).replace(' ', ',')
        text_ls = new_text.split('#')[1:]

        submerged_area_all = {}
        for i,text in tqdm(enumerate(text_ls), desc='淹没面积计算中...', unit='个'):
            with open(result_txt.replace('.txt', '.csv'), 'w') as file:
                file.write(text)
            data_df = pd.read_csv(result_txt.replace('.txt', '.csv'), header=None)
            submerged_area_all[i+1] = cell_sr[data_df[data_df.iloc[:, 2] >= 0.01].iloc[:, 0]].sum()   # 计算淹没面积
        
        max_submerged_area(submerged_area_all, self.output_dir, self.multiple)

    def SichuanHydrodynamic(self):
        # 读取并创建网格
        with open(self.input_dir + "\\mesh.dat", 'r') as file:
            lines = file.readlines()
        coordinate_point = []  # 坐标点
        triangle_vertex = []
        for line in lines:
            nums = re.findall(r'\d+\.\d+', line.strip())
            if len(nums) == 2:
                coordinate_point.append(list(map(lambda x: float(x), nums)))
            num = re.findall(r'\d+', line)
            if len(num) == 3:
                triangle_vertex.append([int(n) for n in num])
        df = pd.DataFrame(coordinate_point, columns=['x', 'y'])
        df.index = range(1, len(df) + 1)

        # 创建几何图形（三角形面）
        # 将三个顶点组合成一个Shapely多边形对象
        geometries = pd.Series(index=range(1, len(triangle_vertex) + 1))
        for n, vertexs in tqdm(enumerate(triangle_vertex), desc='三角网格构建中...', unit='个'):
            # 定义一个三角形的三个顶点
            row1 = df.loc[vertexs[0]]
            row2 = df.loc[vertexs[1]]
            row3 = df.loc[vertexs[2]]
            polygon = Polygon([(row1['x'], row1['y']),
                               (row2['x'], row2['y']),
                               (row3['x'], row3['y'])])
            geometries[n] = polygon

        dats = os.listdir(self.input_dir)
        submerged_area_all = {}
        for d in tqdm(dats, desc='数据转换中...'):
            time = d.replace('.dat', '')
            if re.fullmatch(r'\d+\.dat', d) is not None:
                with open(self.input_dir + r"\{}".format(d), 'r') as file:
                    lines = file.readlines()
                water_depth = []
                for line in lines:
                    nums = re.findall(r'\d+\.\d+', line.strip())
                    if len(nums) > 1:
                        id = re.findall(r'\d+', line)
                        water_depth.append([int(id[0]), float(nums[2])])
                depth_df = pd.DataFrame(water_depth, columns=['id', 'depth'])
                depth_df.set_index('id', inplace=True)

                # 将几何图形列和属性数据组合成一个地理数据框
                gdf = gpd.GeoDataFrame(depth_df['depth'], geometry=geometries[depth_df.index])

                # 设置坐标系
                gdf.set_crs('EPSG:3857', inplace=True)

                # 保存为标准GIS文件格式，即Shapefile
                gdf.to_file(self.output_dir + '\\GIS\\{}.shp'.format(time), driver='ESRI Shapefile')

                # 计算淹没面积
                submerged_area_all[time] = submerged_area_sum(gdf, 'depth')

        max_submerged_area(submerged_area_all, self.output_dir, self.multiple)

    def ShandongHydrodynamic(self):
        input_dats = [self.input_dir + '\\' + tif for tif in os.listdir(self.input_dir) if tif.endswith('.dat')]  # 输入的tif文件路径
        submerged_area_all = {}
        for dat in tqdm(input_dats, desc='数据转换中...', unit='个'):
            # 读取并创建网格
            with open(dat, 'r', encoding='utf-8') as file:
                lines = file.readlines()
            new_lines = ['x1,y1,x2,y2,x3,y3,U,V,H\n'] + lines[1:]  # 修改表头
            with open(dat.replace('.dat', '.csv'), 'w') as file:
                file.writelines(new_lines)

            df = pd.read_csv(dat.replace('.dat', '.csv'))
            os.remove(dat.replace('.dat', '.csv'))

            # 创建几何图形（三角形面）
            # 将三个顶点组合成一个Shapely多边形对象
            geometries = []
            for row in range(len(df)):
                # 定义一个三角形的三个顶点
                polygon = Polygon([((df.loc[row,'x1']), (df.loc[row,'y1'])),
                                   ((df.loc[row,'x2']), (df.loc[row,'y2'])),
                                   ((df.loc[row,'x3']), (df.loc[row,'y3']))])
                geometries.append(polygon)

            # 将几何图形列和属性数据组合成一个地理数据框
            gdf = gpd.GeoDataFrame(df['H'], geometry=geometries)

            # 设置坐标系
            gdf.set_crs('EPSG:3857', inplace=True)

            # 保存为标准GIS文件格式，即Shapefile
            gdf.to_file(self.output_dir + '\\GIS\\{}.shp'.format(dat.split('\\')[-1].replace('.dat', '')), driver='ESRI Shapefile')

            # 计算淹没面积
            submerged_area_all[dat.split('\\')[-1].replace('.dat', '')] = submerged_area_sum(gdf, 'H')

        max_submerged_area(submerged_area_all, self.output_dir, self.multiple)

    def BeijingHydrodynamic(self):
        input_json = self.input_dir + '\\Result.json'
        with open(input_json, 'r', encoding='utf-8') as f:
            data_str = f.read()
            data_ls = json.loads(data_str)

        submerged_area_all = {}
        for data in tqdm(data_ls, desc='数据转换中...'):
            sr = pd.Series(data['h'])
            submerged_count = sr[sr >= 0.01].count()
            submerged_area_all[data['time']] = submerged_count
        max_submerged_area(submerged_area_all, self.output_dir, self.multiple)

    def HainanHydrodynamic(self):
        result_txt = self.input_dir + '\\CellResult.txt'
        with open(result_txt, 'r') as file:
            all_text = file.read()
        
        if len(all_text) == 0:     # 无淹没的情况
            with open(self.output_dir + r'\analysis.csv', 'a', encoding='utf-8') as f:
                f.write("{},{},{}\n".format(self.multiple, 0, 0))
        else:
            new_text = re.sub('\&\d{4}\/\d+\/\d+\s\d+:\d{2}:\d{2}\#', '#', all_text)
            text_ls = new_text.split('#')[1:]

            submerged_area_all = {}
            for i,text in enumerate(text_ls):
                if (i + 6) % 6 == 1:    # 取出水深系列
                    sr = pd.Series([float(s) for s in text.split(',')])
                    submerged_area_all[str(i)] = sr[sr >= 0.01].count()
            max_submerged_area(submerged_area_all, self.output_dir, self.multiple)

    def GansuHydrodynamic(self):
        cell_sr = self._read_cell_info()
        # 计算淹没面积
        result_txt = self.input_dir + '\\CellResult.txt'
        with open(result_txt, 'r') as file:
            all_text = file.read()
        new_text = re.sub('\&\d{4}\/\d+\/\d+\s\d+:\d{2}:\d{2}\#', '#', all_text)
        text_ls = new_text.split('#')[1:]

        submerged_area_all = {}
        for i,text in enumerate(text_ls):
            if (i + 5) % 5 == 0:    # 取出淹没网格序号
                sr = pd.Series([int(s) for s in text.split(',')])
                submerged_area_all[str(i+1)] = cell_sr[sr].sum()
        max_submerged_area(submerged_area_all, self.output_dir, self.multiple)

    def FujianHydrodynamic(self):
        result_txt = self.input_dir + '\\CellResult.txt'
        with open(result_txt, 'r') as file:
            all_text = file.read()
        
        if len(all_text) == 0:     # 无淹没的情况
            with open(self.output_dir + r'\analysis.csv', 'a', encoding='utf-8') as f:
                f.write("{},{},{}\n".format(self.multiple, 0, 0))
        else:
            new_text = re.sub('\&\d{4}\/\d+\/\d+\s\d+:\d{2}:\d{2}\#', '#', all_text)
            text_ls = new_text.split('#')[1:]

            submerged_area_all = {}
            for i,text in enumerate(text_ls):
                if (i + 4) % 4 == 1:    # 取出水深系列
                    sr = pd.Series([float(s) for s in text.split(',')])
                    submerged_area_all[str(i)] = sr[sr >= 0.01].count()
            max_submerged_area(submerged_area_all, self.output_dir, self.multiple) 

    def ShaanxiHydrodynamic(self):
        # 读取网格信息
        cell_sr = self._read_cell_info()

        # 计算淹没面积
        result_txt = self.input_dir + '\\CellResult.txt'
        with open(result_txt, 'r',encoding='utf-8') as file:
            all_text = file.read()

        pattern = "\d{4}-\d{2}-\d{2}\s\d{2}:\d{2}:\d{2}\n"
        new_text = re.sub(pattern, '#', all_text).replace(' ', ',')
        text_ls = new_text.split('#')[1:]

        submerged_area_all = {}
        for i,text in tqdm(enumerate(text_ls), desc='淹没面积计算中...', unit='个'):
            with open(result_txt.replace('.txt', '.csv'), 'w') as file:
                file.write(text)
            data_df = pd.read_csv(result_txt.replace('.txt', '.csv'), header=None)
            sr = data_df.iloc[:, 2]   # 取出水深列
            sr.index = data_df.iloc[:, 0].values  # 设置网格编号
            submerged_area_all[i+1] = cell_sr[sr >= 0.01].sum()   # 计算淹没面积
        
        max_submerged_area(submerged_area_all, self.output_dir, self.multiple)

    def NingxiaHydrodynamic(self):
        # 读取网格信息
        cell_path = os.path.dirname(os.path.dirname(self.input_dir)) + '\\csv\\WG.csv'
        cell_df = pd.read_csv(cell_path, encoding='utf-8',header=None,skiprows=1)
        cell_df_new = cell_df.iloc[:,[0,5]]
        cell_df_new.columns = ['id', 'area']

        # 计算淹没面积
        result_jsons = [self.input_dir + '\\' + file for file in os.listdir(self.input_dir) if 'Time' in file]
        submerged_area_all = {}
        for result_json in result_jsons:
            with open(result_json, 'r', encoding='utf-8') as f:
                data_str = f.read()
                data_dic = json.loads(data_str)

            if len(data_dic['features']) != 0:
                data_df = pd.DataFrame(data_dic['features'])
                submerged_area_all[result_json.split('\\')[-1].replace('.json', '')] = cell_df_new[cell_df_new['id'].isin(data_df[data_df['h']>0.01]['id'])]['area'].sum()

        max_submerged_area(submerged_area_all, self.output_dir, self.multiple)

    def XinjiangHydrodynamic(self):
        self.GansuHydrodynamic()

    def AnhuiHydrodynamic(self):
        self.GansuHydrodynamic()

    def XizangHydrodynamic(self):
        asc_files = [file for file in os.listdir(self.input_dir) if re.match(r'h_\d+\.asc', file)]
        submerged_area_all = {}
        for f in asc_files:
            submerged_area_all[f.split('_')[-1].replace('.asc', '')] = asc_to_shp(self.input_dir+'\\'+f).sum()
        max_submerged_area(submerged_area_all, self.output_dir, self.multiple)

    def HubeiHydrodynamic(self):
        submerged_area_all = parse_dat_file(self.input_dir + '\\' + "X2DProcess.dat")
        max_submerged_area(submerged_area_all, self.output_dir, self.multiple)

    def HunanHydrodynamic(self):
        self.HubeiHydrodynamic()

    def YunnanHydrodynamic(self):
        self.HubeiHydrodynamic()

    def GuangxiHydrodynamic(self):
        with open(self.input_dir + '\\' + "lj007_result.json", 'r', encoding='utf-8') as f:
            data_str = f.read()
            data_dic = json.loads(data_str)

        area_time = {}
        for data in data_dic['data']:
            for a in data['data']:
                if a['time'] not in area_time.keys():
                    area_time[a['time']] = 0
                if a['H'] > 0.01:
                    area_time[a['time']] += 1

        max_submerged_area(area_time, self.output_dir, self.multiple)

    def JiangxiHydrodynamic(self):
        txt = [self.input_dir + '\\' + tif for tif in os.listdir(self.input_dir) if tif.endswith('.txt')]
        with open(txt[0], 'r') as file:
            all_text = file.read()
            data_dic = json.loads(all_text)

        submerged_area_all = pd.Series(data_dic["ModelRunnerList"][0]["HArray"])
        max_submerged_area(submerged_area_all, self.output_dir, self.multiple)
        os.remove(txt[0])

    def BingtuanHydrodynamic(self):
        txt = [self.input_dir + '\\' + tif for tif in os.listdir(self.input_dir) if tif.endswith('.txt')]
        submerged_area_all = {}
        for t in txt:
            df = pd.read_csv(t, header=None, sep='\t')
            submerged_area_all[t.split('\\')[-1].replace('.txt', '')] = df[df.iloc[:, 2] >= 0.01].iloc[:,2].count()

        max_submerged_area(submerged_area_all, self.output_dir, self.multiple)

    def JilinHydrodynamic(self):
        self.JiangxiHydrodynamic()

    def HenanHydrodynamic(self):
        self.JiangxiHydrodynamic()

    def ShanxiHydrodynamic(self):
        self.HubeiHydrodynamic()

    def GuangdongSubmerged(self):
        input_tifs = [self.input_dir + '\\' + tif for tif in os.listdir(self.input_dir) if tif.endswith('.tif')]  # 输入的tif文件路径
        threshold_value = 0.01  # 阈值，根据实际需要修改
        submerged_area_all = {}
        for input_tif in input_tifs:
            # 执行转换和统计
            time = input_tif.split('\\')[-1].split('_')[0]
            gdf = tif_to_shp(input_tif, self.output_dir + '\\GIS\\{}.shp'.format(time), threshold_value)
            submerged_area_all[time] = submerged_area_sum(gdf, 'depth')

        max_submerged_area(submerged_area_all, self.output_dir, self.multiple)

    def HeilongjiangSubmerged(self):
        timeseries = [file for file in os.listdir(self.input_dir) if file.endswith('.json')]
        submerged_area_all = {}
        for t in timeseries:
            time = t.replace('.json', '').split('_')[-1]
            gdf = json_to_shp(self.input_dir + '\\' + t)
            # gdf = gdf.to_crs('EPSG:3857', inplace=True)
            # area = gpd.GeoSeries(gdf.geometry.area)
            submerged_area_all[time] = gdf.area.sum()
            gdf.to_file(self.output_dir + '\\GIS\\{}.shp'.format(time), encoding='utf-8')
        max_submerged_area(submerged_area_all, self.output_dir, self.multiple)

    def FujianSubmerged(self):
        asc_files = [file for file in os.listdir(self.input_dir) if file.endswith('depth.asc')]
        submerged_area_all = {}
        for f in asc_files:
            submerged_area_all[f.replace('.asc', '')] = asc_to_shp(self.input_dir+'\\'+f).sum()
        max_submerged_area(submerged_area_all, self.output_dir, self.multiple)

    def ShandongSubmerged(self):
        self.HeilongjiangSubmerged()     # 使用和黑龙江相同的方法

    def ZhejiangSubmerged(self):
        input_tifs = [self.input_dir + '\\' + tif for tif in os.listdir(self.input_dir) if tif.endswith('depth.tif')]  # 输入的tif文件路径
        threshold_value = 0.01  # 阈值，根据实际需要修改
        submerged_area_all = {}
        for input_tif in input_tifs:
            # 执行转换和统计
            time = input_tif.split('\\')[-1].replace('.tif', '')
            gdf = tif_to_shp(input_tif, self.output_dir + '\\GIS\\{}.shp'.format(time), threshold_value)
            submerged_area_all[time] = submerged_area_sum(gdf, 'depth')

        max_submerged_area(submerged_area_all, self.output_dir, self.multiple)

    def Hunan1Submerged(self):
        self.FujianSubmerged()

    def Hunan2Submerged(self):
        self.FujianSubmerged()

    def AnhuiSubmerged(self):
        self.FujianSubmerged()

    def GuangxiSubmerged(self):
        self.FujianSubmerged()

    def GansuSubmerged(self):
        self.FujianSubmerged()

    def QinghaiSubmerged(self):
        self.FujianSubmerged()

    def NingxiaSubmerged(self):
        input_tifs = [tif for tif in os.listdir(self.input_dir) if tif.endswith('.JSON')]  # 输入的tif文件路径
        submerged_area_all = {}
        for input_json in tqdm(input_tifs, desc='数据转换中...', unit='个'):
            # 执行转换和统计
            with open(self.input_dir + '\\' + input_json, 'r', encoding='utf-8') as f:
                data_str = f.read()
                data_dic = json.loads(data_str)
            data_df = pd.DataFrame(data_dic['m_flooded_region'],columns=['x','y','depth','t','water_level','elevation','i','j'])
            
            key = input_json.replace('.JSON', '').split('_')[-1]
            if key in submerged_area_all.keys():
                submerged_area_all[key] = submerged_area_all[key] + data_df[data_df['depth'] >= 0.01]['i'].count()
            else:
                submerged_area_all[key] = data_df[data_df['depth'] >= 0.01]['i'].count()   # 计算淹没面积

        max_submerged_area(submerged_area_all, self.output_dir, self.multiple)

    def LiaoningSubmerged(self):
        self.HeilongjiangSubmerged()

    def JilinSubmerged(self):
        self.FujianSubmerged()

    def JiangxiSubmerged(self):
        self.FujianSubmerged()

    def HenanSubmerged(self):
        self.FujianSubmerged()

    def HebeiSubmerged(self):
        self.FujianSubmerged()

    def BeijingSubmerged(self):
        self.FujianSubmerged()

    def Shanxi2Submerged(self):
        asc_files = [file for file in os.listdir(self.input_dir) if file.endswith('depth_5m.asc')]
        submerged_area_all = {}
        for f in asc_files:
            submerged_area_all[f.replace('.asc', '')] = asc_to_shp(self.input_dir + '\\' + f).sum()
        max_submerged_area(submerged_area_all, self.output_dir, self.multiple)

    def Xinjiang3Submerged(self):
        self.FujianSubmerged()

    def XizangSubmerged(self):
        self.FujianSubmerged()

    def ChongqingSubmerged(self):
        self.FujianSubmerged()

    def HainanSubmerged(self):
        self.FujianSubmerged()


if __name__ == '__main__':
    function_name = sys.argv[1]
    result_in_dir = sys.argv[2]
    result_out_dir = sys.argv[3]
    multiple = sys.argv[4]
    post = PostProcessing(result_in_dir, result_out_dir, multiple)
    exec("post.{}()".format(function_name))

    # post = PostProcessing(r"D:\Models_test\Submerged\Heilongjiang\results\20260129154752196",
    #                       r"D:\Models_test\Result\Submerged\Heilongjiang\001_WAB140012L000000", 6)
    # post.JilinSubmerged()