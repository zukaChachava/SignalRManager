import React, {useState} from 'react';
import {Context, Hub} from "react-signalr/lib/signalr/types";
import {useForm} from "react-hook-form";
import {Button, Card, CardContent, CardHeader, Container, List, Stack, TextField, Typography} from "@mui/material";

type DirectMessageProps = {
    context: Context<Hub<string, string>>
}

type MessageForm = {
    userId: number
    message: string;
}

function DirectMessage({context}: DirectMessageProps) {
    const {register, handleSubmit} = useForm<MessageForm>();
    const [messages, setMessages] = useState<Array<string>>([]);

    context.useSignalREffect('SendMessageToUser', (userId: string, message: string) => {
        console.log('here')
        setMessages(previous => [...previous, message])
    }, [messages]);

    const onSubmit = (message: MessageForm) => {
        console.log(message)
        context.invoke('SendMessageToUser', message.userId, message.message);
    }

    return (
        <Card sx={{width: '100%', height: '100%'}}>
            <CardHeader title={'Direct Messages'}/>
            <CardContent>
                <form onSubmit={handleSubmit(onSubmit)}>
                    <Stack direction={"row"}>
                        <TextField label={'User Id'} type={"number"} {...register('userId', {required: true})} />
                        <TextField label={'Message'} {...register('message', {required: true})} />
                    </Stack>
                    <Button type={"submit"}>Send</Button>
                </form>
            </CardContent>
            <CardContent>
                <List>
                    {messages.map((message, index) => (
                        <Container key={index} disableGutters={true}
                                   sx={{borderRadius: '40px', marginTop: '10px'}}>
                            <Typography variant={"body1"}>
                                {message}
                            </Typography>
                        </Container>
                    ))}
                </List>
            </CardContent>
        </Card>
    );
}

export default DirectMessage;