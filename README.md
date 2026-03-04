Documentação nomus
https://documenter.getpostman.com/view/22813773/2s93JutNgM#37657879-dbf3-487d-a546-0b5b075f3dd7


Instalando .exe no serviços do windows
sc.exe create ".NET nomus-sync Service" binpath="C:\Users\Arthur\source\repos\TaskManager\TaskManager.API\bin\Release\net8.0\win-x64\publish\TaskManager.API.exe"
sc.exe create ".NET nomus-sync Service" binpath="C:\Letmesee\Jobs\.NET nomus-sync\lms-nomus-erp-synchronizer-job.exe"
Removendo serviço do windows
sc.exe delete ".NET nomus-sync Service"