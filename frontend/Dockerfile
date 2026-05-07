# Dockerfile for Digital Twin Frontend
FROM node:20-slim

WORKDIR /app

# Copy package files first for layer caching
COPY package*.json ./

# Install dependencies
RUN npm install --production

# Copy the rest of the frontend source
COPY . .

# Expose the default port (3000)
EXPOSE 3000

# Start the server
CMD ["npm", "start"]