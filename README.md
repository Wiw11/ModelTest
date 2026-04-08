# 水专业模型运行测试

## 项目概述
本项目基于.NET 9.0 开发，用于对水科院提供的两类水专业模型进行运行测试。

### 模型类型
- 水动力学模型
    - 标识：Hydrodynamic
    - 原文件路径：`D:\Projects\水科院模型\水动力学模型`
    - 测试目录：`.\Hydrodynamic\[省份]\[实例]`
    - 运行结果目录：`.\Hydrodynamic\[省份]\Result\[实例]`
- 简化淹没模型
    - 标识：Submerged
    - 原文件路径：`D:\Projects\水科院模型\简化淹没模型`
    - 测试目录：`.\Submerged\[省份]\[实例]`
    - 运行结果目录：`.\Submerged\[省份]\Result\[实例]`

### 项目结构
```
ModelTest/
├── ModelInfo.cs          # 数据模型定义
├── AppConfig.cs          # 项目配置数据模型
├── ModelConfigLoader.cs  # 配置模型加载器
├── SingleModelTest.cs    # 单模型测试基类
├── ModelsTest.cs         # 具体省份模型测试类
├── ModelsTestController.cs      # 单模型多实例测试控制器
├── ExternalProcessRunner.cs     # 外部进程执行器
├── Tools.cs              # 工具类
├── Program.cs            # 主入口
├── ModelTest.csproj      # 项目文件
├── README.md             # 项目说明
├── model_main.csv        # 模型配置信息
├── convertion.py         # 数据转换
├── postprocessing.py     # 后处理脚本
├── application.json      # 项目配置文件
└── pyproject.toml        # Python环境配置文件
```

## 功能特性

### 核心功能
- **前处理**: 文件复制、配置修改、流量参数调整
- **模型运行**: 支持多种执行方式（.exe, .bat, .jar, .py等文件）
- **后处理**: GIS格式转换、淹没面积计算

## 构建要求

- .NET 9.0 或更高版本
- Python 3.8+（用于后处理脚本）

## 构建和运行

```bash
# 进入项目目录
cd ModelTest

# 运行示例
dotnet run Guizhou Hydrodynamic   # 循环运行贵州省水动力学模型的多个实例
dotnet run Guangdong Submerged true    # 异步运行广东省简化淹没模型的多个实例
dotnet run Shandong Hydrodynamic     # 运行山东省水动力模型的002_WEB03008T0000000实例
```

## 命令行参数

| 参数位置 | 含义 | 是否必含 | 说明 |
|------|------|-----|-----|
| 1 | 省份 | 是 | 省份全拼，如`Guizhou` |
| 2 | 模型类型 | 是 | 包括水动力和淹没两类，分别对应`Hydrodynamic`和`Submerged` |
| 3 | 执行模式 | 否 | 传入`true`时异步执行多个实例，传入`false`时循环执行多个实例，传入示例名称时执行指定单个实例。默认为`false` |
