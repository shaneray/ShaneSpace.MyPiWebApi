ShaneSpace.MyPiWebApi
=====================
A test application for running on RaspberryPi.

Run WebApi
----------
https://docs.microsoft.com/en-us/aspnet/core/host-and-deploy/linux-nginx?view=aspnetcore-5.0

*Deploy Application To PI*
Copy application to: `/var/www/ShaneSpaceMyPiWebApi`

*Install Nginx*
Install: `sudo apt install nginx`
Start Service: `sudo service nginx start`

*Configure Nginx*
Edit Configuration file: `/etc/nginx/sites-available/default`
```
server {
    listen        80;
    #server_name   example.com *.example.com;
    location / {
        proxy_pass         http://127.0.0.1:5000;
        proxy_http_version 1.1;
        proxy_set_header   Upgrade $http_upgrade;
        proxy_set_header   Connection keep-alive;
        proxy_set_header   Host $host;
        proxy_cache_bypass $http_upgrade;
        proxy_set_header   X-Forwarded-For $proxy_add_x_forwarded_for;
        proxy_set_header   X-Forwarded-Proto $scheme;
    }
}
```

Reload Nginx config: `sudo nginx -s reload`

*Manage Kestrel Process*
Create service file: `sudo touch /etc/systemd/system/kestrel-ShaneSpaceMyPiWebApi.service`
```
[Unit]
Description=ShaneSpace MyPiWebApi

[Service]
WorkingDirectory=/var/www/ShaneSpace.MyPiWebApi.Web
ExecStart=/usr/bin/dotnet /var/www/ShaneSpace.MyPiWebApi.Web/ShaneSpace.MyPiWebApi.Web.dll
Restart=always
# Restart service after 10 seconds if the dotnet service crashes:
RestartSec=10
KillSignal=SIGINT
SyslogIdentifier=ShaneSpaceMyPiWebApi
User=www-data
Environment=ASPNETCORE_ENVIRONMENT=Production
Environment=DOTNET_PRINT_TELEMETRY_MESSAGE=false

[Install]
WantedBy=multi-user.target
```

Enable Service: `sudo systemctl enable kestrel-ShaneSpaceMyPiWebApi.service`
Start the service: `sudo systemctl start kestrel-ShaneSpaceMyPiWebApi.service`


Restart the service: `sudo systemctl restart kestrel-ShaneSpaceMyPiWebApi.service`