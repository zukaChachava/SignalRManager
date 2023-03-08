import React, {useState} from 'react';
import {Box, Button, Container, Divider, TextField} from "@mui/material";
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
        <>
            <Box height={20} />
            <Container
                disableGutters={true}
                sx={{width: '100%'}}
            >
                <Container sx={{display: 'flex', flexDirection: 'row', alignItems: 'center', width: '100%'}}>
                    <form onSubmit={handleSubmit(onSubmit)} style={{display: 'flex', justifyContent: 'start', alignItems: 'center', gap: '10px'}}>
                        <TextField label={'UserId'} type={"number"} {...register('id', {required: true})} />
                        <Button type={"submit"}>Connect</Button>
                    </form>
                    <Box height={20}/>
                    <SignalRUsersHub connectionId={connectionId}/>
                </Container>
            </Container>
            <Box height={20}/>
            <Divider />
        </>
    );
}

export default ConnectionBox;