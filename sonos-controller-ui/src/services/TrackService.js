import axios from 'axios';

const getApiUrl = () => {
  const host = window.location.host; // e.g., "localhost:3000" or "192.168.0.31:3000"

  if (host.startsWith("localhost")) {
    return "https://localhost:7134/api/";
  } else {
    var hostParts = host.split(':');
    return `http://${hostParts[0]}:8080/api/`;
  }

  return "https://default-api-url.com/api/"; // Fallback API URL (modify as needed)
};

const API_URL = getApiUrl();

export const getDevices = async (reset) => {
  try {
    const response = await axios.get(API_URL + `device?reset=${reset}`);
    return response.data;
  } catch (error) {
    console.error('Error fetching devices:', error);
    throw error; // Re-throw the error if you want to handle it further up the call stack
  }
};

export const getStatus = async (ip) => {
  try {
    let url = API_URL + `track/status/${ip}`;
    const response = await axios.get(url);
    return response.data;
  } catch (error) {
    console.error('Error fetching track status:', error);
    throw error; // Re-throw the error if you want to handle it further up the call stack
  }
};

export const getTrack = async (ip) => {
  try {
    let url = API_URL + `track/${ip}`;
    const response = await axios.get(url);
    return response.data;
  } catch (error) {
    console.error('Error fetching track:', error);
    throw error; // Re-throw the error if you want to handle it further up the call stack
  }
};

export const updateTrack = async (ip) => {
  const response = await axios.put(API_URL + `track/${ip}`);
  return response.data.message;
};

export const deleteTrack = async (ip) => {
  const response = await axios.delete(API_URL + `track/${ip}`);
  return response.data;
};

export const playTrack = async (ip) => {
  const response = await axios.put(API_URL + `track/play/${ip}`);
  return response.data;
};
export const pauseTrack = async (ip) => {
  const response = await axios.put(API_URL + `track/pause/${ip}`);
  return response.data;
};
export const skipTrack = async (ip) => {
  const response = await axios.put(API_URL + `track/skip/${ip}`);
  return response.data;
};
export const advanceTrack = async (ip, secs) => {
  const response = await axios.put(API_URL + `track/advance/${ip}?secs=${secs}`);
  return response.data;
};
