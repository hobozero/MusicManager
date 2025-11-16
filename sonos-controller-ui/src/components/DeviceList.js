import React, { useEffect, useState, useContext } from 'react';
import { getDevices } from '../services/TrackService';
import { ActiveDeviceContext } from '../ActiveDeviceContext';
import { Grid, Button, Card, CardContent, Typography, CardActionArea} from '@mui/material';

const DeviceList = () => {
  const [devices, setDevices] = useState([]);
  const { activeDeviceIp, setActiveDeviceIp } = useContext(ActiveDeviceContext);

  const fetchDevices = async (reset = false) => {
      const response = await getDevices(reset);
      setDevices(response);
  };

  useEffect(() => {
    fetchDevices(false);
  }, []);

  const handleDeviceClick = (ipAddress) => {
    setActiveDeviceIp(ipAddress);
  };

  return (
    <div className="p-4">
      <Button
        variant="contained"
        color="primary"
        onClick={() => fetchDevices(true)}
        sx={{ mb: 2 }}
      >
        Refresh Rooms
      </Button>
      <Grid container spacing={3}>
        {devices.map((device, index) => (
          <Grid item xs={12} sm={6} md={4} lg={3} key={index}>
            <Card
              onClick={() => handleDeviceClick(device.ipAddress)}
              className={ activeDeviceIp === device.ipAddress ? 'activeIp' : '#inactiveIp' }
            >
              <CardActionArea>
                <CardContent>
                  <Typography variant="h6" component="div">
                    {device.roomName}
                  </Typography>
                  <Typography variant="body2" color="textSecondary">
                    {device.ipAddress}
                  </Typography>
                </CardContent>
              </CardActionArea>
            </Card>
          </Grid>
        ))}
      </Grid>
    </div>
  );
};

export default DeviceList;
