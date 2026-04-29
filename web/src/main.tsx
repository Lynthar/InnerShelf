import { ApolloProvider } from '@apollo/client/react'
import { StrictMode } from 'react'
import { createRoot } from 'react-dom/client'
import { App } from './App'
import { apolloClient } from './apollo'
import './index.css'

const root = document.getElementById('root')
if (!root) throw new Error('Missing #root mount node')

createRoot(root).render(
  <StrictMode>
    <ApolloProvider client={apolloClient}>
      <App />
    </ApolloProvider>
  </StrictMode>,
)
