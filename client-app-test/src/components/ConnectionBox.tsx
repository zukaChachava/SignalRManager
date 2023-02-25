import React, {useState} from 'react';
import {Box, Button, Container, TextField} from "@mui/material";
import {useForm} from "react-hook-form";
import SignalRUsersHub from "./SignalRUsersHub";

type ConnectionId = {
    id: number
}

function ConnectionBox() {
    const {register, handleSubmit} = useForm<ConnectionId>();
    const [connectionId, setConnectionId] = useState<number>(0);

    const onSubmit = (connectionId: ConnectionId) => {
        setConnectionId(connectionId.id);
    }

    return (
        <Container
            disableGutters={true}
            sx={{width: '100%'}}
        >
            <Container sx={{display: 'flex', flexDirection: 'column', alignItems: 'center'}}>
                <form onSubmit={handleSubmit(onSubmit)} style={{display: 'flex', justifyContent: 'start', alignItems: 'center', gap: '10px'}}>
                    <TextField type={"number"} {...register('id', {required: true})} />
                    <Button type={"submit"}>Connect</Button>
                </form>
                <Box height={20}/>
                <SignalRUsersHub connectionId={connectionId}/>
            </Container>
        </Container>
    );
}

export default ConnectionBox;