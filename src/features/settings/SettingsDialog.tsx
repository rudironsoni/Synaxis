import { useState, useEffect } from 'react'
import Modal from '@/components/ui/Modal'
import useSettingsStore from '@/stores/settings'

export default function SettingsDialog({ open, onClose }:{ open:boolean; onClose:()=>void }){
  const gatewayUrl = useSettingsStore((s)=>s.gatewayUrl)
  const costRate = useSettingsStore((s)=>s.costRate)
  const setGatewayUrl = useSettingsStore((s)=>s.setGatewayUrl)
  const setCostRate = useSettingsStore((s)=>s.setCostRate)

  const [url, setUrl] = useState(gatewayUrl)
  const [rate, setRate] = useState(costRate)

  useEffect(()=>{
    setUrl(gatewayUrl)
    setRate(costRate)
  },[gatewayUrl, costRate])

  const save = ()=>{
    setGatewayUrl(url)
    setCostRate(Number(rate))
    onClose()
  }

  return (
    <Modal open={open} onClose={onClose} title="Settings">
      <div className="flex flex-col gap-3">
        <label className="text-sm">Gateway URL</label>
        <input value={url} onChange={(e)=>setUrl(e.target.value)} className="w-full rounded px-2 py-1 bg-[var(--input)] text-[var(--input-foreground)]" />

        <label className="text-sm">Cost Rate ($ per 1k tokens)</label>
        <input type="number" step="0.01" value={rate} onChange={(e)=>setRate(Number(e.target.value))} className="w-full rounded px-2 py-1 bg-[var(--input)] text-[var(--input-foreground)]" />

        <div className="pt-2 flex justify-end">
          <button onClick={save} className="px-3 py-1 rounded bg-[var(--primary)] text-[var(--primary-foreground)]">Save</button>
        </div>
      </div>
    </Modal>
  )
}
