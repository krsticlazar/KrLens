import { fireEvent, render, screen, waitFor } from '@testing-library/react'
import App from './App'
import * as api from './services/api'

vi.mock('./services/api', () => ({
  uploadImage: vi.fn(),
  applyFilter: vi.fn(),
  downloadImage: vi.fn(),
  deleteSession: vi.fn(),
}))

const uploadImageMock = vi.mocked(api.uploadImage)
const applyFilterMock = vi.mocked(api.applyFilter)
const downloadImageMock = vi.mocked(api.downloadImage)
const deleteSessionMock = vi.mocked(api.deleteSession)

describe('App', () => {
  beforeEach(() => {
    vi.clearAllMocks()

    let counter = 0
    vi.stubGlobal('URL', {
      ...globalThis.URL,
      createObjectURL: vi.fn(() => `blob:mock-${++counter}`),
      revokeObjectURL: vi.fn(),
    })

    uploadImageMock.mockResolvedValue({ sessionId: 'session-1', width: 640, height: 480 })
    applyFilterMock.mockResolvedValue(new Blob(['filtered'], { type: 'image/png' }))
    downloadImageMock.mockResolvedValue(new Blob(['preview'], { type: 'image/png' }))
    deleteSessionMock.mockResolvedValue(undefined)
  })

  afterEach(() => {
    vi.unstubAllGlobals()
  })

  it('prikazuje preview nakon što je fajl selektovan', async () => {
    render(<App />)

    const fileInput = screen.getByLabelText(/učitaj sliku/i)
    fireEvent.change(fileInput, {
      target: {
        files: [new File(['image'], 'test.png', { type: 'image/png' })],
      },
    })

    await waitFor(() =>
      expect(screen.getByAltText(/trenutni preview/i)).toHaveAttribute('src', 'blob:mock-1'),
    )
  })

  it('drži apply dugme disabled kada nema session-a', () => {
    render(<App />)

    expect(screen.getByRole('button', { name: /primeni filter/i })).toBeDisabled()
  })

  it('drži undo dugme disabled kada nema istorije', () => {
    render(<App />)

    expect(screen.getByRole('button', { name: /undo/i })).toBeDisabled()
  })

  it('drži redo dugme disabled kada nema future stanja', () => {
    render(<App />)

    expect(screen.getByRole('button', { name: /redo/i })).toBeDisabled()
  })

  it('revert vraća original i čisti istoriju', async () => {
    uploadImageMock
      .mockResolvedValueOnce({ sessionId: 'session-1', width: 640, height: 480 })
      .mockResolvedValueOnce({ sessionId: 'session-2', width: 640, height: 480 })

    render(<App />)

    const fileInput = screen.getByLabelText(/učitaj sliku/i)
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

    expect(screen.getByRole('button', { name: /undo/i })).toBeEnabled()

    fireEvent.click(screen.getByRole('button', { name: /vrati original/i }))

    await waitFor(() =>
      expect(screen.getByAltText(/trenutni preview/i)).toHaveAttribute('src', 'blob:mock-1'),
    )

    expect(screen.getByRole('button', { name: /undo/i })).toBeDisabled()
  })
})
