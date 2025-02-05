# source: https://github.com/tiangolo/nginx-rtmp-docker
FROM tiangolo/nginx-rtmp

# Update the package list and install OpenSSL
RUN apt-get update && apt-get install -y openssl

COPY nginx.conf /etc/nginx/nginx.conf
COPY index.html /www/
