import React from 'react';
import { Helmet } from 'react-helmet';
import TrackPlayer from './components/TrackPlayer';
import DeviceList from './components/DeviceList';
import { CssBaseline } from '@mui/material';
import { ActiveDeviceProvider } from './ActiveDeviceContext';

function App() {
  const apiUrl = 'http://192.168.0.31:8080';

  return (
    
    <ActiveDeviceProvider>
      <div className="App">
        <CssBaseline />
        <TrackPlayer />
        <main>
          <DeviceList />
        </main>
      </div>
    </ActiveDeviceProvider>
  );
}

export default App;
