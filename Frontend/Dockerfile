
ARG REGISTRY=docker.io

FROM ${REGISTRY}/node:24.11-alpine3.21 AS builder
ARG NUXT_PUBLIC_API_BASE
RUN npm install -g pnpm

WORKDIR /app

COPY ./package.json ./pnpm-lock.yaml pnpm-workspace.yaml ./
RUN pnpm install --frozen-lockfile

COPY . .

# expose build-time environment variable to the Node build process
ENV NUXT_PUBLIC_API_BASE=${NUXT_PUBLIC_API_BASE}

RUN pnpm build

FROM ${REGISTRY}/node:24.11-alpine3.21
WORKDIR /app

COPY --from=builder /app/.output ./.output

EXPOSE 3000

CMD [ "node", ".output/server/index.mjs" ]
