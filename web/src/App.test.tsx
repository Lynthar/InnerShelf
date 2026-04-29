import { MockedProvider } from '@apollo/client/testing/react'
import { render, screen } from '@testing-library/react'
import { describe, expect, it } from 'vitest'
import { App } from './App'

describe('App', () => {
  it('renders the env-not-configured hint when no API key is set', () => {
    // stashConfig is captured at module load; in this test env VITE_STASH_API_KEY
    // is unset, so the app should show the configuration hint.
    render(
      <MockedProvider mocks={[]}>
        <App />
      </MockedProvider>,
    )

    expect(screen.getByRole('heading', { name: /innershelf/i })).toBeInTheDocument()
    expect(screen.getByText(/VITE_STASH_API_KEY/)).toBeInTheDocument()
  })
})
