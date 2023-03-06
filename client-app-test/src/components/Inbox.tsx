import React, {useState} from 'react';
import {Context, Hub} from "react-signalr/lib/signalr/types";
import {Card, CardContent, CardHeader, Container, List, Typography} from "@mui/material";

type InboxProps = {
    context: Context<Hub<string, string>>
}

function Inbox({context}: InboxProps) {
    const [messages, setMessages] = useState<Array<string>>([]);

    context.useSignalREffect('SendMessageToGroup', (message: string) => {
        setMessages(previous => [...previous, message])
    }, []);

    return (
        <Card  sx={{width: '100%', height: '100%', overflowY: 'auto'}}>
            <CardHeader title={'Group Messages'} />
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

export default Inbox;