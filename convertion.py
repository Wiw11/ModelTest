# -*- coding: UTF-8 -*-
'''
数据转换工具函数
@Author：Cao Duanxiang
@Date：2026/01/07
'''
import os
import json
import struct
import numpy as np
import pandas as pd
import matplotlib.pyplot as plt
import geopandas as gpd
import rasterio
from rasterio.features import shapes
from typing import List, Dict, Tuple

def submerged_area_sum(shp,title):
    '''
    基于shpfile网格数据计算总淹没面积
    Parameters
    ----------
    shp: str | DataFrame
        shpfile文件路径或已加载的数据框
    title: str
        水深列列名
    Returns
    -------
    float
        总淹没面积
    '''
    if isinstance(shp, gpd.geodataframe.GeoDataFrame):
        gdf = shp
    elif isinstance(shp, str):
        gdf = gpd.read_file(shp, encoding='gbk')
    else:
        raise ValueError('shpfile must be DataFrame or str')
    gdf['area'] = gdf.area
    area = gdf[gdf[title] >= 0.01]['area']
    submerged_area = area.sum()

    return submerged_area

def max_submerged_area(submerged_area_all,output_dir,multiple):   # 计算最大淹没面积并记录
    count_sr = pd.Series(submerged_area_all)
    max_area = count_sr.max()  # 最大淹没面积
    max_time = count_sr.idxmax()  # 最大淹没出现时间
    with open(output_dir + r'\analysis.csv', 'a', encoding='utf-8') as f:
        f.write("{},{},{}\n".format(multiple, max_area, max_time))

def json_to_shp(json_file):
    '''
    将json文件转换为shpfile文件
    Parameters
    ----------
    json_file: str
        json文件路径

    Returns
    -------
    GeoDataFrame
        转换得到的空间数据框
    '''
    gdf = gpd.read_file(json_file, encoding='utf-8')

    return gdf

def tif_to_shp(tif_path, output_shp_path, threshold, band=1):
    """
    将tif格式文件转换为shp格式，并统计值大于指定阈值的面积

    参数:
    tif_path: 输入的tif文件路径
    output_shp_path: 输出的shp文件路径
    threshold: 阈值，用于统计大于此值的区域
    band: 波段号，默认为1
    """
    # 读取tif文件
    with rasterio.open(tif_path) as src:
        # 读取指定波段的数据
        image = src.read(band)
        # 获取空间变换参数
        transform = src.transform
        # 获取坐标参考系统
        crs = src.crs

        # 将大于阈值的像素设为1，其他设为0（创建掩膜）
        mask = image > threshold

        # 使用rasterio的shapes函数将栅格转换为矢量
        # 这里只处理值大于阈值的区域
        results = (
            {'geometry': shape, 'properties': {'id': 123, 'depth': value}}
            for shape, value in shapes(image, mask=mask, transform=transform)
            if value > threshold
        )

        # 创建GeoDataFrame
        geoms = list(results)
        if geoms:
            gdf = gpd.GeoDataFrame.from_features(geoms, crs=crs)
        else:
            # 如果没有满足条件的区域，创建空的GeoDataFrame
            gdf = gpd.GeoDataFrame(columns=['geometry'], crs=crs)

    # 保存为shp文件
    gdf.to_file(output_shp_path)

    return gdf

def asc_to_shp(asc_file):
    with rasterio.open(asc_file) as src:
        data = src.read(1)
        nodata = src.nodata
    # 过滤NODATA值
    data_masked = np.ma.masked_equal(data, nodata)
    # 转换为Series
    sr = pd.Series(data_masked.flatten())

    return sr

def json_to_df(path):
    # 读取json为DataFrame格式
    with open(path, 'r', encoding='utf-8') as f:
        data_str = f.read()
        data_dict = json.loads(data_str)
        json_df = pd.DataFrame(data_dict)
    return json_df

def visualize_depth(gdf, title, output_png_path):
    '''
    基于GeoDataFrame绘制淹没图
    Parameters
    ----------
    gdf: gpd.GeoDataFrame
        GeoDataFrame数据框
    title: str
        水深列字段名
    output_png_path: str
        输出png图片的绝对路径

    '''
    fig, ax = plt.subplots()
    gdf.plot(ax=ax, column=title, cmap='viridis', legend=True)
    fig.savefig(output_png_path)
    plt.close()

def parse_dat_file(file_path: str) -> Dict:
    '''
    解析.dat二进制文件
    
    Parameters
    ----------
    file_path : str
        .dat文件路径
    
    Returns
    -------
    dict

    '''
    result = {
        'cellcount': 0,
        'time_steps': []
    }

    depth_time = {}
    
    with open(file_path, 'rb') as f:
        # 读取文件头
        # 第一个int (4字节，无意义标识)
        marker1 = struct.unpack('i', f.read(4))[0]
        
        # 第二个int (4字节，无意义标识)
        marker2 = struct.unpack('i', f.read(4))[0]
        
        # cellcount (4字节，网格数量)
        cellcount = struct.unpack('i', f.read(4))[0]
        result['cellcount'] = cellcount
        
        # int64 (8字节，无意义标识)
        marker3 = struct.unpack('q', f.read(8))[0]
        
        # 循环读取每个时间步的数据
        time_step_count = 0
        while True:
            # 读取当前时间 t (double, 8字节)
            t_bytes = f.read(8)
            if len(t_bytes) < 8:
                break  # 文件结束
            
            t = struct.unpack('d', t_bytes)[0]
            
            # 读取该时间步所有网格单元的数据
            grid_data = []
            for i in range(cellcount):
                # 每个网格单元6个float变量: zi, h, u, v, speed, angle
                cell_bytes = f.read(4 * 6)  # 24字节
                if len(cell_bytes) < 24:
                    raise ValueError(f"时间步 {t}: 网格单元 {i} 数据不完整")
                
                zi, h, u, v, speed, angle = struct.unpack('ffffff', cell_bytes)
                grid_data.append({
                    'grid_id': i,
                    'zi': zi,      # 水位
                    'h': h,        # 水深
                    'u': u,        # x方向速度
                    'v': v,        # y方向速度
                    'speed': speed, # 合速度
                    'angle': angle  # 合速度方向
                })
            
            # 转换为DataFrame
            df = pd.DataFrame(grid_data)
            
            depth_time[str(time_step_count)] = df[df['h'] > 0.01]['h'].sum()
            
            time_step_count += 1
            if time_step_count % 10 == 0:
                print(f"已解析 {time_step_count} 个时间步...")
    
    print(f"\n解析完成!")
    print(f"总时间步数: {len(result['time_steps'])}")
    
    return depth_time