# Explicitly use Node.js for the x86 architecture
FROM node:18

# Set the working directory
WORKDIR /usr/src/app

# Copy the package files and install dependencies
COPY package*.json ./
RUN npm install

# Copy the rest of the application code
COPY . .

# Expose the port your application will run on
EXPOSE 3000

# Command to run your app
CMD ["node", "server.js"]
