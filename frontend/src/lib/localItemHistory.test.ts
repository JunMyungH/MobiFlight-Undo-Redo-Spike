
import {
  describe,
  expect,
  it,
} from 'vitest'

import {
  createLocalItemHistory,
  currentLocalItem,
  recordLocalItemChange,
  undoLocalItem,
  redoLocalItem,
  resizeLocalItemHistory,
} from './localItemHistory'

type Item = {
  id: string
  name: string
  active: boolean
}

const initial: Item = {
  id: 'item-1',
  name: 'Initial',
  active: false,
}

const sameItem = (a: Item, b: Item) =>
  a.id === b.id &&
  a.name === b.name &&
  a.active === b.active

function rename(
  history: ReturnType<
    typeof createLocalItemHistory<Item>
  >,
  name: string,
) {
  return recordLocalItemChange(
    history,
    {
      ...currentLocalItem(history),
      name,
    },
    'Edit Name',
    sameItem,
  )
}

describe('Local Item Snapshot History', () => {
  it('restores Undo and Redo states', () => {
    let history = createLocalItemHistory(initial, 10)

    history = rename(history, 'First')
    history = rename(history, 'Second')

    history = undoLocalItem(history)

    expect(currentLocalItem(history).name)
      .toBe('First')

    history = redoLocalItem(history)

    expect(currentLocalItem(history).name)
      .toBe('Second')
  })

  it('removes the oldest history when full', () => {
    let history = createLocalItemHistory(initial, 2)

    history = rename(history, 'A')
    history = rename(history, 'B')
    history = rename(history, 'C')

    expect(history.actions).toHaveLength(2)
    expect(history.snapshots).toHaveLength(3)

    history = undoLocalItem(history)
    history = undoLocalItem(history)

    expect(currentLocalItem(history).name)
      .toBe('A')

    expect(undoLocalItem(history)).toBe(history)
  })

  it('clears Redo after a new edit', () => {
    let history = createLocalItemHistory(initial, 10)

    history = rename(history, 'A')
    history = rename(history, 'B')
    history = undoLocalItem(history)

    history = rename(history, 'C')

    expect(history.cursor).toBe(2)
    expect(history.actions).toHaveLength(2)
    expect(redoLocalItem(history)).toBe(history)
  })

  it('does not record unchanged values', () => {
    let history = createLocalItemHistory(initial, 10)

    history = rename(history, 'A')
    history = undoLocalItem(history)

    const unchanged = rename(history, 'Initial')

    expect(unchanged).toBe(history)
    expect(history.actions).toHaveLength(1)
  })

  it('respects a reduced history limit', () => {
    let history = createLocalItemHistory(initial, 50)

    for (let i = 1; i <= 40; i++) {
      history = rename(history, `Edit ${i}`)
    }

    history = resizeLocalItemHistory(history, 10)

    expect(history.actions).toHaveLength(10)
    expect(history.snapshots).toHaveLength(11)
    expect(history.cursor).toBe(10)

    for (let i = 0; i < 10; i++) {
      history = undoLocalItem(history)
    }

    expect(currentLocalItem(history).name)
      .toBe('Edit 30')
  })
})
