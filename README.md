# Hangifre HttpJob 调度服务

基于`.net core 6.0`，独立服务，编译后直接运行`WinInstall.bat`

## WinInstall.bat 中的配置

默认服务名称`Hangfire.Service`, `binpath`中通过指定参数`-p 3810` 设置程序访问端口为3810

> 注意，服务安装运行后，运行的`Environment`是`Production`，注意配置文件