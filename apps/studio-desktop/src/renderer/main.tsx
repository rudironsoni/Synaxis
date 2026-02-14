import React from 'react'
import ReactDOM from 'react-dom/client'
import { TamaguiProvider } from '@tamagui/core'
import config from '../../tamagui.config'
import App from './App'
import './index.css'

ReactDOM.createRoot(document.getElementById('root')!).render(
  <React.StrictMode>
    <TamaguiProvider config={config} defaultTheme="dark">
      <App />
    </TamaguiProvider>
  </React.StrictMode>
)
