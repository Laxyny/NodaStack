FROM php:8.2-apache

RUN docker-php-ext-install mysqli pdo pdo_mysql

COPY ./index.php /var/www/html/index.php

RUN mkdir -p /var/www/html/www
VOLUME ["/var/www/html/www"]