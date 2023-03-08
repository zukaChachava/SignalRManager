import React, {useState} from 'react';
import {createSignalRContext} from "react-signalr";
import {Box, Container, Stack, Typography} from "@mui/material";
import Groups from "./Groups";
import Inbox from "./Inbox";
import DirectMessage from "./DirectMessage";

const SignalRContext = createSignalRContext();
const url = "https://localhost:7081/hub/users"

type SignalRUserHubProps = {
    connectionId: number
}

function SignalRUsersHub({connectionId}: SignalRUserHubProps) {
    const [connectionOpened, setConnectionOpened] = useState<boolean>(false);

    if(connectionId === 0)
        return <></>;

    return (
        <SignalRContext.Provider
            url={url}
            onOpen={() => setConnectionOpened(true)}
            onClosed={() => setConnectionOpened(false)}
            accessTokenFactory={() => connectionId.toString()}
            dependencies={[connectionId]}
        >
            {
                connectionOpened && (
                    <Container sx={{display: 'flex', flexDirection: 'column', alignItems: 'center'}}>
                        <Typography color={"green"}>Connection Opened</Typography>
                        <Groups context={SignalRContext} />
                        <Box height={5} />
                        <Stack direction={"row"} spacing={2} sx={{width: '100%'}}>
                            <Container disableGutters={true} sx={{width: '50%', minHeight: '200px', maxWidth: '400px', overflowY: 'auto'}}>
                                <DirectMessage context={SignalRContext}/>
                            </Container>
                            <Container disableGutters={true} sx={{width: '50%', minHeight: '200px', maxHeight: '400px', overflowY: 'auto'}}>
                                <Inbox context={SignalRContext} />
                            </Container>
                        </Stack>
                    </Container>
                )}
        </SignalRContext.Provider>
    );
}

export default SignalRUsersHub;