import React, { createContext, useState } from 'react';

export const ActiveDeviceContext = createContext();

export const ActiveDeviceProvider = ({ children }) => {
  const [activeDeviceIp, setActiveDeviceIp] = useState(null);

  return (
    <ActiveDeviceContext.Provider value={{ activeDeviceIp, setActiveDeviceIp }}>
      {children}
    </ActiveDeviceContext.Provider>
  );
};
