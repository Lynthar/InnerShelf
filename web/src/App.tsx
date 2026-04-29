import { useQuery } from '@apollo/client/react'
import { stashConfig } from './apollo'
import { FIND_SCENES } from './queries/scenes'

interface SceneFile {
  path: string
}

interface Scene {
  id: string
  title: string | null
  details: string | null
  date: string | null
  rating100: number | null
  files: SceneFile[]
}

interface FindScenesData {
  findScenes: {
    count: number
    scenes: Scene[]
  }
}

export function App() {
  const { data, loading, error } = useQuery<FindScenesData>(FIND_SCENES, {
    variables: { filter: { per_page: 20 } },
    skip: !stashConfig.hasApiKey,
  })

  if (!stashConfig.hasApiKey) {
    return (
      <main className="p-8">
        <h1 className="text-2xl font-semibold">InnerShelf</h1>
        <p className="mt-4 text-sm opacity-80">
          No <code>VITE_STASH_API_KEY</code> set. Copy <code>.env.example</code> to{' '}
          <code>.env.local</code> and fill in a key generated from the Stash admin UI at{' '}
          <code>{stashConfig.url}</code>.
        </p>
      </main>
    )
  }

  if (loading) {
    return <main className="p-8">Loading scenes…</main>
  }

  if (error) {
    return (
      <main className="p-8">
        <h1 className="text-2xl font-semibold">InnerShelf</h1>
        <p className="mt-4 text-sm text-red-500">Query failed: {error.message}</p>
      </main>
    )
  }

  const count = data?.findScenes.count ?? 0
  const scenes = data?.findScenes.scenes ?? []

  console.log('[Phase 1] findScenes returned', count, 'scenes:', scenes)

  return (
    <main className="p-8">
      <h1 className="text-2xl font-semibold">InnerShelf</h1>
      <p className="mt-2 text-sm opacity-70">
        {count} scenes total — showing first {scenes.length}. See devtools console for raw payload.
      </p>
      <ul className="mt-6 space-y-2">
        {scenes.map((scene) => (
          <li key={scene.id} className="rounded border border-neutral-700 px-3 py-2 text-sm">
            <span className="font-mono opacity-60">#{scene.id}</span>{' '}
            {scene.title ?? <em className="opacity-50">(untitled)</em>}
          </li>
        ))}
      </ul>
    </main>
  )
}
