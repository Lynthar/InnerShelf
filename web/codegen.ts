import type { CodegenConfig } from '@graphql-codegen/cli'

const stashUrl = process.env.VITE_STASH_URL ?? 'http://localhost:9999'
const apiKey = process.env.VITE_STASH_API_KEY ?? ''

const config: CodegenConfig = {
  schema: [
    {
      [`${stashUrl}/graphql`]: {
        headers: apiKey ? { ApiKey: apiKey } : {},
      },
    },
  ],
  documents: ['src/**/*.{ts,tsx}', '!src/gql/**/*'],
  generates: {
    'src/gql/': {
      preset: 'client',
      presetConfig: {
        gqlTagName: 'graphql',
      },
    },
  },
  ignoreNoDocuments: true,
  hooks: {
    afterAllFileWrite: ['biome format --write'],
  },
}

export default config
