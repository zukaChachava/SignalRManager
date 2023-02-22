import React, {useState} from 'react';
import {createSignalRContext} from "react-signalr";
import {Container, Typography} from "@mui/material";

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
                    <Container>
                        <Typography color={"green"}>Connection Opened</Typography>
                    </Container>
                )}
        </SignalRContext.Provider>
    );
}

export default SignalRUsersHub;