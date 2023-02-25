import React, {useState} from 'react';
import {createSignalRContext} from "react-signalr";
import {Container, Typography} from "@mui/material";
import Groups from "./Groups";

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
                        <Container>
                            <Groups context={SignalRContext} />
                        </Container>
                    </Container>
                )}
        </SignalRContext.Provider>
    );
}

export default SignalRUsersHub;