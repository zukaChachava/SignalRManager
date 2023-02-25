import React from 'react';
import {Button, TextField} from "@mui/material";
import {useForm} from "react-hook-form";
import {Context, Hub} from "react-signalr/lib/signalr/types";

type GroupMessageFormProps = {
    groupName: string
    context: Context<Hub<string, string>>
}

type GroupMessageData = {
    message: string
}

function GroupMessageForm({context, groupName}: GroupMessageFormProps) {
    const {register, handleSubmit} = useForm<GroupMessageData>();

    const onSubmit = (data: GroupMessageData) => {
        context.invoke('SendMessageToGroup', groupName, data.message);
    }

    return (
        <form onSubmit={handleSubmit(onSubmit)} style={{display: 'flex', flexDirection: 'row', alignItems: 'center'}}>
            <TextField label={'message'} {...register('message', {required: true})}/>
            <Button type={'submit'}>Send Message</Button>
        </form>
    );
}

export default GroupMessageForm;