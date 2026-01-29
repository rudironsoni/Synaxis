// simple in-memory mock for Dexie-based db used in tests
type AnyObj = Record<string, any>

function makeTable<T extends AnyObj>(){
  let items: T[] = []
  return {
    reset(){ items = [] },
    toArray: async ()=> items.slice(),
    add: async (obj: Partial<T>)=>{ const id = (items.length? (items[items.length-1] as any).id + 1 : 1); const item = { ...obj, id } as T; items.push(item); return id },
    delete: async (id:number)=> { items = items.filter((i:any)=>i.id !== id) },
    where(){ return { equals: (v:number)=> ({ toArray: async ()=> items.filter((i:any)=>i.sessionId === v) }) } }
  }
}

const sessions = makeTable<any>()
const messages = makeTable<any>()

export default {
  sessions,
  messages,
}

export { sessions, messages }
