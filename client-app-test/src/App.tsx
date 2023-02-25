import React, {useState} from 'react';
import './App.css';
import {Button, Container, createTheme, CssBaseline, List, ThemeProvider} from "@mui/material";
import ConnectionBox from "./components/ConnectionBox";

const theme = createTheme({
  palette:{
    mode: "dark"
  }
})

function App() {
  const [id, setId] = useState<number>(0);
  const [connections, setConnections] = useState<Array<number>>([]);

  const addConnection = () => {
    setConnections(previous =>  [...previous, id]);
    setId(previous => previous + 1);
  }

  return (
      <ThemeProvider theme={theme}>
        <CssBaseline />
        <Container disableGutters={true} sx={{width: '100%', display: 'flex', flexDirection: 'column', alignItems: 'center'}}>
          <Button onClick={() => addConnection()}>Add new Connection</Button>
          <List>
            {connections.map(connection =>
                <Container key={connection} sx={{margin: '10px 0'}}>
                    <ConnectionBox/>
                </Container>
            )}
          </List>
        </Container>
      </ThemeProvider>
  );
}

export default App;
