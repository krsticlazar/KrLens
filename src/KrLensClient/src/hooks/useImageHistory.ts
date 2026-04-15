import { useMemo, useReducer } from 'react'
import type { ImageSnapshot } from '../types'

const MAX_HISTORY = 3

interface HistoryState {
  past: ImageSnapshot[]
  current: ImageSnapshot | null
  future: ImageSnapshot[]
}

type HistoryAction =
  | { type: 'push'; snapshot: ImageSnapshot }
  | { type: 'undo' }
  | { type: 'redo' }
  | { type: 'reset'; snapshot: ImageSnapshot | null }
  | { type: 'revert'; snapshot: ImageSnapshot | null }

function reducer(state: HistoryState, action: HistoryAction): HistoryState {
  switch (action.type) {
    case 'push': {
      if (!state.current) {
        return { past: [], current: action.snapshot, future: [] }
      }

      return {
        past: [...state.past, state.current].slice(-MAX_HISTORY),
        current: action.snapshot,
        future: [],
      }
    }
    case 'undo': {
      if (!state.current || state.past.length === 0) {
        return state
      }

      const previous = state.past[state.past.length - 1]
      return {
        past: state.past.slice(0, -1),
        current: previous,
        future: [state.current, ...state.future].slice(0, MAX_HISTORY),
      }
    }
    case 'redo': {
      if (!state.current || state.future.length === 0) {
        return state
      }

      const next = state.future[0]
      return {
        past: [...state.past, state.current].slice(-MAX_HISTORY),
        current: next,
        future: state.future.slice(1),
      }
    }
    case 'reset':
      return { past: [], current: action.snapshot, future: [] }
    case 'revert':
      return { past: [], current: action.snapshot, future: [] }
    default:
      return state
  }
}

export function useImageHistory(initialSnapshot: ImageSnapshot | null) {
  const [state, dispatch] = useReducer(reducer, {
    past: [],
    current: initialSnapshot,
    future: [],
  })

  return useMemo(
    () => ({
      ...state,
      push: (snapshot: ImageSnapshot) => dispatch({ type: 'push', snapshot }),
      undo: () => dispatch({ type: 'undo' }),
      redo: () => dispatch({ type: 'redo' }),
      reset: (snapshot: ImageSnapshot | null) => dispatch({ type: 'reset', snapshot }),
      revert: (snapshot: ImageSnapshot | null) => dispatch({ type: 'revert', snapshot }),
      canUndo: state.past.length > 0,
      canRedo: state.future.length > 0,
      peekUndo: state.past[state.past.length - 1] ?? null,
      peekRedo: state.future[0] ?? null,
    }),
    [state],
  )
}
