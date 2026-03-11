FROM node:22-slim

# Install claude-code globally
RUN npm install -g @anthropic-ai/claude-code

WORKDIR /app
COPY package.json server.js ./

EXPOSE 4500
CMD ["node", "server.js"]
