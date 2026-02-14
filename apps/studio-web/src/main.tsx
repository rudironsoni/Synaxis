import React from 'react';
import ReactDOM from 'react-dom/client';
import { BrowserRouter } from 'react-router-dom';
import { config } from '@synaxis/ui';
import { TamaguiProvider } from '@tamagui/core';
import App from './App';
import './index.css';

ReactDOM.createRoot(document.getElementById('root')!).render(
  <React.StrictMode>
    <TamaguiProvider config={config} defaultTheme="light">
      <BrowserRouter>
        <App />
      </BrowserRouter>
    </TamaguiProvider>
  </React.StrictMode>
);
