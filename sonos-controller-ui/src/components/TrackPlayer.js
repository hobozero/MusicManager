import React, { useState, useEffect, useContext } from 'react';
import { getStatus, getTrack, updateTrack, playTrack, pauseTrack, skipTrack, deleteTrack, advanceTrack } from '../services/TrackService';
import { ActiveDeviceContext } from '../ActiveDeviceContext';
import { Container, Typography, Button, Box, Card, CardContent, CardMedia } from '@mui/material';
import PlayCircleOutline from '@mui/icons-material/PlayCircleOutline';
import PauseCircleOutline from '@mui/icons-material/PauseCircleOutline';
import SkipNext from '@mui/icons-material/SkipNext';
import FastForward from '@mui/icons-material/FastForward';
import FastRewind from '@mui/icons-material/FastRewind';
import IconButton from '@mui/material/IconButton';

const TrackPlayer = () => {
  const [track, setTrack] = useState(null);
  const [isPlaying, setIsPlaying] = useState(false);
  const [currentTime, setCurrentTime] = useState(0);
  const [saveResponse, setSaveResponse] = useState(null);
  const { activeDeviceIp } = useContext(ActiveDeviceContext);

  useEffect(() => {
    if (activeDeviceIp) {
      const fetchState = async () => {
        await fetchTrack();
        await fetchStatus();
      };

      fetchState();

      const interval = setInterval(() => {
        if (isPlaying){
          fetchState();
        }
      }, 5000);

      return () => clearInterval(interval); // Cleanup interval on unmount
    }
    //Re-runs when a value changes
  }, [activeDeviceIp, isPlaying]);

  useEffect(() =>{

    
    //Re-runs when a value changes
  }, [isPlaying])

  const fetchTrack = async () => {
    const data = await getTrack(activeDeviceIp);
    setTrack(data);
    setCurrentTime(data.currentPlayTime);
  };

  const fetchStatus = async () => {
    const status = await getStatus(activeDeviceIp);
    var isPlaying = status.currentTransportState === "PLAYING" && status.currentTransportStatus === "OK";
    console.log(`TransportState: ${status.currentTransportState} TransportStatus: ${status.currentTransportStatus}`);
    setIsPlaying(isPlaying);
  }

  const handlePlay = async () => {
    setSaveResponse("");
    setIsPlaying(true);
    let track = await playTrack(activeDeviceIp);
    setTrack(track);
  };

  const handlePause = async () => {
    setSaveResponse("");
    await setIsPlaying(false);
    let track = pauseTrack(activeDeviceIp);
    setTrack(track);
  };

  const handleSave = async () => {
    if (track) {
      try {
        setSaveResponse("processing...");
        let response = await updateTrack(activeDeviceIp);
        setSaveResponse(response);
      } catch (error) {
        setSaveResponse("Failed to save time.");
      }
    }
  };

  const handleSkip = async () => {
    setSaveResponse("");
    var track = await skipTrack(activeDeviceIp);
    setTrack(track);
  };

  const handleDelete = async () => {
    if (track) {
      try {
        setSaveResponse("processing...");
        let response = await deleteTrack(activeDeviceIp);
        setSaveResponse(response);
        await fetchTrack();
      } catch (error) {
        setSaveResponse("Failed to save time.");
      }
    }
  };

  const jumpSeconds = async (secs) =>{
    let track = await advanceTrack(activeDeviceIp, secs);
    setTrack(track);
  }

  return (
    <Container maxWidth="sm">
      {track && (
        <Card>
          <CardContent>
            <Typography variant="h5" component="div">
              {track.title}
            </Typography>
            <Typography variant="subtitle1" color="text.secondary">
              {track.artist} - {track.album}
            </Typography>
            <Typography variant="subtitle1" color="text.secondary">
              {track.currentPlayTime} of {track.totalPlayTime}
            </Typography>
            <Box mt={2} display="flex" alignItems="center">
            <IconButton color="primary" aria-label={isPlaying ? 'pause' : 'play'} onClick={isPlaying ? handlePause : handlePlay}>
              {isPlaying ? <PauseCircleOutline /> : <PlayCircleOutline />}
            </IconButton>
            <IconButton color="primary" aria-label="skip" onClick={handleSkip}>
              { <SkipNext />}
            </IconButton>
            <IconButton color="primary" aria-label="skip" onClick={() => jumpSeconds(-10)}>
              { <FastRewind />}
            </IconButton>
            <IconButton color="primary" aria-label="skip" onClick={() => jumpSeconds(10)}>
              { <FastForward />}
            </IconButton>

              <Button variant="contained" color="primary" onClick={handleSave}>
                Save
              </Button>
              <Button
                variant="contained"
                color="primary"
                onClick={ handleDelete }
              >
                {'Crop'}
              </Button> 
            
              </Box>
            <Box mt={2} display="flex" alignItems="center">
              {saveResponse && (
                  <Typography variant="body2" color="text.secondary" sx={{ ml: 2 }}>
                    {saveResponse}
                  </Typography>
                )}
            </Box>
          </CardContent>
        </Card>
      )}
    </Container>
  );
};

export default TrackPlayer;
