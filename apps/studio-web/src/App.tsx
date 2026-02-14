import React from 'react';
import { Routes, Route, Navigate } from 'react-router-dom';
import { useChatStore } from './store/chatStore';
import Login from './pages/Login';
import Chat from './pages/Chat';
import Settings from './pages/Settings';
import Layout from './components/Layout';

function App() {
  const isAuthenticated = useChatStore((state) => state.isAuthenticated);

  if (!isAuthenticated) {
    return <Login />;
  }

  return (
    <Layout>
      <Routes>
        <Route path="/" element={<Navigate to="/chat" replace />} />
        <Route path="/chat" element={<Chat />} />
        <Route path="/settings" element={<Settings />} />
      </Routes>
    </Layout>
  );
}

export default App;
