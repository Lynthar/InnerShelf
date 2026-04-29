import { gql } from '@apollo/client'

// Phase 1 sanity query: pulls a small page of scenes to verify the GraphQL
// pipeline (Apollo client + auth header + Stash schema). Once `pnpm codegen`
// has run against a live Stash, replace `gql` with the generated `graphql`
// tag from `src/gql/` for full type safety.
export const FIND_SCENES = gql`
  query FindScenes($filter: FindFilterType) {
    findScenes(filter: $filter) {
      count
      scenes {
        id
        title
        details
        date
        rating100
        files {
          path
        }
      }
    }
  }
`
