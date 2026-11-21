# CalendarOcr en el servidor Ubuntu (con Ollama)

## 1. Instalar .NET 10
```
sudo apt update
sudo apt install -y dotnet-sdk-10.0
```
Si dice que no encuentra el paquete:
```
sudo add-apt-repository ppa:dotnet/backports
sudo apt update
sudo apt install -y dotnet-sdk-10.0
```

## 2. Subir el proyecto
Con MobaXterm, arrastra la carpeta `CalendarOcr.Api` a `/home/gader/`.

## 3. Compilar
```
cd /home/gader/CalendarOcr.Api
dotnet publish -c Release -o /home/gader/calendarocr
```

## 4. Probar a mano
Cambia `MiCodigoSecreto` por el código que quieras usar en la web:
```
App__Token=MiCodigoSecreto ASPNETCORE_URLS=http://0.0.0.0:5000 dotnet /home/gader/calendarocr/CalendarOcr.Api.dll
```
Abre en el móvil o en el PC: `http://IP-del-servidor:5000`
Para pararlo: `Ctrl + C`.

## 5. Dejarlo como servicio (arranca solo al encender)
```
sudo nano /etc/systemd/system/calendarocr.service
```
Pega esto (cambia el token):
```
[Unit]
Description=CalendarOcr
After=network-online.target ollama.service

[Service]
User=gader
WorkingDirectory=/home/gader/calendarocr
ExecStart=/usr/bin/dotnet /home/gader/calendarocr/CalendarOcr.Api.dll
Environment=ASPNETCORE_URLS=http://0.0.0.0:5000
Environment=App__Token=MiCodigoSecreto
Restart=always

[Install]
WantedBy=multi-user.target
```
Guarda (`Ctrl + O`, `Intro`, `Ctrl + X`) y actívalo:
```
sudo systemctl daemon-reload
sudo systemctl enable --now calendarocr
```
Ver si funciona: `systemctl status calendarocr`
Ver errores: `journalctl -u calendarocr -n 50 --no-pager`

## Actualizar después de cambiar código
```
cd /home/gader/CalendarOcr.Api
dotnet publish -c Release -o /home/gader/calendarocr
sudo systemctl restart calendarocr
```
