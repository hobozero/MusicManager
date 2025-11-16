# Use an ARM-compatible Node.js image
FROM --platform=linux/arm64 node:18 AS build
WORKDIR /app

# Copy package.json and package-lock.json and install dependencies
COPY package*.json ./
RUN npm install

# Copy the rest of the application and build it
COPY . ./
RUN npm run build

# Use an ARM-compatible Nginx image
FROM --platform=linux/arm64 nginx:alpine
COPY --from=build /app/build /usr/share/nginx/html

# Expose port 80
EXPOSE 80

# Copy custom Nginx configuration
COPY nginx.conf /etc/nginx/nginx.conf
