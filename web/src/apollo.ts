import { ApolloClient, HttpLink, InMemoryCache, split } from '@apollo/client'
import { GraphQLWsLink } from '@apollo/client/link/subscriptions'
import { getMainDefinition } from '@apollo/client/utilities'
import { createClient } from 'graphql-ws'

const STASH_URL = import.meta.env.VITE_STASH_URL ?? 'http://localhost:9999'
const STASH_API_KEY = import.meta.env.VITE_STASH_API_KEY ?? ''

const httpUrl = `${STASH_URL}/graphql`
const wsUrl = httpUrl.replace(/^http/, 'ws')

const headers: Record<string, string> = STASH_API_KEY ? { ApiKey: STASH_API_KEY } : {}

const httpLink = new HttpLink({ uri: httpUrl, headers })

const wsLink = new GraphQLWsLink(
  createClient({
    url: wsUrl,
    connectionParams: STASH_API_KEY ? { ApiKey: STASH_API_KEY } : undefined,
  }),
)

const link = split(
  ({ query }) => {
    const def = getMainDefinition(query)
    return def.kind === 'OperationDefinition' && def.operation === 'subscription'
  },
  wsLink,
  httpLink,
)

export const apolloClient = new ApolloClient({
  link,
  cache: new InMemoryCache(),
})

export const stashConfig = {
  url: STASH_URL,
  hasApiKey: STASH_API_KEY.length > 0,
}
