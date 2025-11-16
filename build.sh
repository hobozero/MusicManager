# in /SonosManager
docker build -t sonosmanagerapi:latest .

# build for TNAS
docker buildx build --platform linux/arm64 -t sonosmanagerapi_arm:latest .
# publish to TNAS
docker save -o \\192.168.0.31\images\sonosmanagerapi_arm.tar sonosmanagerapi_arm:latest

# PuTTY into 192.168.0.31
# sudo -i
# chmod 644 sonosmanagerapi_arm.tar
# docker run -d --network host -p 8080:8080 sonosmanagerapi_arm:latest

# THIS WORKS
# Log into the TOS web insterface
# open Docker app
# import the image
# launch container with 8080:8080 and network host


# in /sonos-controller-ui
# docker build -t sonosmanagerui:latest .
# docker save -o \\192.168.0.31\images\sonosmanagerui_arm.tar sonosmanagerui_arm:latest

npm run build
copy to 192.168.0.31/web/sonosmgr
# I added this to nginx.conf

    # React app server
    server {
        listen 3000;
        server_name your-tnas-ip;  # replace with your TNAS IP

        root /mnt/md0/web/sonosmgr;
        index index.html;

        location / {
            try_files $uri /index.html;
        }
    }





# test in isolation
# docker run -d --name my_container --network my_bridge_network my_image
# docker run -it -p 7134:8080 sonosmanagerapi:latest  --network sonos_network
# open 7134





