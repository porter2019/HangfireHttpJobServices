# Hangifre HttpJob Windows 调度服务

windows服务器下使用这个程序来调度任务，linux下使用 [Docker](https://github.com/yuzd/Hangfire.HttpJob/wiki/000.Docker-Quick-Start) 来运行，

基于`.net core 8.0`，独立服务，发布后直接运行`WinInstall.bat`

## WinInstall.bat 中的配置

默认服务名称`Hangfire.Service`, `binpath`中通过指定参数`-p 3810` 设置程序访问端口为3810

> 注意，服务安装运行后，运行的`Environment`是`Production`，注意配置文件
> 
> 安装运行后的环境是Production的，使用的是`appsettings.json`和`nlog.config`，Development配置使用不到