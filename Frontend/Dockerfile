FROM node:24.11-alpine3.21 AS builder
RUN npm install -g pnpm

WORKDIR /app

COPY ./package.json ./pnpm-lock.yaml pnpm-workspace.yaml ./
RUN pnpm install --frozen-lockfile

COPY . .
RUN pnpm build

FROM node:24.11-alpine3.21

WORKDIR /app

COPY --from=builder /app/.output ./.output

CMD [ "node", ".output/server/index.mjs" ]
