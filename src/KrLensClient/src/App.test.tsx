import { fireEvent, render, screen, waitFor } from '@testing-library/react'
import App from './App'
import * as api from './services/api'
import type { SessionState } from './types'

vi.mock('./services/api', () => ({
  uploadImage: vi.fn(),
  applyFilter: vi.fn(),
  downloadImage: vi.fn(),
  getSessionState: vi.fn(),
  undoSession: vi.fn(),
  redoSession: vi.fn(),
  revertSession: vi.fn(),
  rotateSession: vi.fn(),
  deleteSession: vi.fn(),
}))

const uploadImageMock = vi.mocked(api.uploadImage)
const applyFilterMock = vi.mocked(api.applyFilter)
const downloadImageMock = vi.mocked(api.downloadImage)
const getSessionStateMock = vi.mocked(api.getSessionState)
const undoSessionMock = vi.mocked(api.undoSession)
const redoSessionMock = vi.mocked(api.redoSession)
const revertSessionMock = vi.mocked(api.revertSession)
const rotateSessionMock = vi.mocked(api.rotateSession)
const deleteSessionMock = vi.mocked(api.deleteSession)

function buildState(overrides: Partial<SessionState> = {}): SessionState {
  return {
    sessionId: 'session-1',
    width: 640,
    height: 480,
    currentStep: 0,
    maxHistory: 50,
    canUndo: false,
    canRedo: false,
    history: [],
    ...overrides,
  }
}

describe('App', () => {
  beforeEach(() => {
    vi.clearAllMocks()

    let counter = 0
    vi.stubGlobal('URL', {
      ...globalThis.URL,
      createObjectURL: vi.fn(() => `blob:mock-${++counter}`),
      revokeObjectURL: vi.fn(),
    })

    uploadImageMock.mockResolvedValue({
      sessionId: 'session-1',
      width: 640,
      height: 480,
      state: buildState(),
    })
    applyFilterMock.mockResolvedValue(new Blob(['filtered'], { type: 'image/png' }))
    downloadImageMock.mockResolvedValue(new Blob(['preview'], { type: 'image/png' }))
    getSessionStateMock.mockResolvedValue(buildState())
    undoSessionMock.mockResolvedValue(new Blob(['undo'], { type: 'image/png' }))
    redoSessionMock.mockResolvedValue(new Blob(['redo'], { type: 'image/png' }))
    revertSessionMock.mockResolvedValue(new Blob(['revert'], { type: 'image/png' }))
    rotateSessionMock.mockResolvedValue(new Blob(['rotate'], { type: 'image/png' }))
    deleteSessionMock.mockResolvedValue(undefined)
  })

  afterEach(() => {
    vi.unstubAllGlobals()
  })

  it('prikazuje preview nakon sto je fajl selektovan', async () => {
    render(<App />)

    const fileInput = screen.getByLabelText(/sliku/i)
    fireEvent.change(fileInput, {
      target: {
        files: [new File(['image'], 'test.png', { type: 'image/png' })],
      },
    })

    await waitFor(() =>
      expect(screen.getByAltText(/trenutni preview/i)).toHaveAttribute('src', 'blob:mock-1'),
    )
  })

  it('drzi apply dugme disabled kada nema session-a', () => {
    render(<App />)

    expect(screen.getByRole('button', { name: /primeni filter/i })).toBeDisabled()
  })

  it('drzi undo dugme disabled kada nema istorije', () => {
    render(<App />)

    expect(screen.getByRole('button', { name: /undo/i })).toBeDisabled()
  })

  it('drzi redo dugme disabled kada nema future stanja', () => {
    render(<App />)

    expect(screen.getByRole('button', { name: /redo/i })).toBeDisabled()
  })

  it('revert vraca original preko server-side sesije', async () => {
    getSessionStateMock
      .mockResolvedValueOnce(
        buildState({
          currentStep: 1,
          canUndo: true,
          history: [{ step: 1, filter: 'Invert', parameters: null }],
        }),
      )
      .mockResolvedValueOnce(
        buildState({
          currentStep: 0,
          canUndo: false,
          canRedo: true,
          history: [{ step: 1, filter: 'Invert', parameters: null }],
        }),
      )

    render(<App />)

    const fileInput = screen.getByLabelText(/sliku/i)
    fireEvent.change(fileInput, {
      target: {
        files: [new File(['image'], 'test.png', { type: 'image/png' })],
      },
    })

    await waitFor(() =>
      expect(screen.getByAltText(/trenutni preview/i)).toHaveAttribute('src', 'blob:mock-1'),
    )

    fireEvent.click(screen.getByRole('button', { name: /primeni filter/i }))

    await waitFor(() =>
      expect(screen.getByAltText(/trenutni preview/i)).toHaveAttribute('src', 'blob:mock-2'),
    )

    fireEvent.click(screen.getByRole('button', { name: /vrati original/i }))

    await waitFor(() =>
      expect(screen.getByAltText(/trenutni preview/i)).toHaveAttribute('src', 'blob:mock-3'),
    )

    expect(revertSessionMock).toHaveBeenCalledWith('session-1')
  })

  it('rotacija desno azurira preview i dimenzije bez history koraka', async () => {
    getSessionStateMock.mockResolvedValueOnce(
      buildState({
        width: 480,
        height: 640,
        canUndo: false,
        canRedo: false,
      }),
    )

    render(<App />)

    const fileInput = screen.getByLabelText(/sliku/i)
    fireEvent.change(fileInput, {
      target: {
        files: [new File(['image'], 'test.png', { type: 'image/png' })],
      },
    })

    await waitFor(() =>
      expect(screen.getByAltText(/trenutni preview/i)).toHaveAttribute('src', 'blob:mock-1'),
    )

    fireEvent.click(screen.getByRole('button', { name: /rotiraj desno/i }))

    await waitFor(() =>
      expect(screen.getByAltText(/trenutni preview/i)).toHaveAttribute('src', 'blob:mock-2'),
    )

    expect(rotateSessionMock).toHaveBeenCalledWith('session-1')
    expect(screen.getByText(/Format: PNG \| 480 x 640/i)).toBeInTheDocument()
    expect(screen.getByRole('button', { name: /undo/i })).toBeDisabled()
    expect(screen.getByRole('button', { name: /redo/i })).toBeDisabled()
  })
})
