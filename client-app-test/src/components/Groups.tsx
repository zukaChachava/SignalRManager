import React, {useState} from 'react';
import {
    Box,
    Button,
    Card,
    CardContent,
    CardHeader,
    Container,
    List,
    ListItem, ListItemButton,
    ListItemText,
    TextField
} from "@mui/material";
import {Context, Hub} from "react-signalr/lib/signalr/types";
import {useForm} from "react-hook-form";
import GroupMessageForm from "./GroupMessageForm";

type GroupsProps = {
    context: Context<Hub<string, string>>
}

type JoinGroupProp = {
    groupName: string
}

function Groups({context}: GroupsProps) {
    const {register, handleSubmit} = useForm<JoinGroupProp>()
    const [groups, setGroups] = useState<Array<string>>([]);

    const onSubmit = (data: JoinGroupProp) => {
        context.invoke("JoinGroup", data.groupName);
        setGroups(previous => [...previous, data.groupName]);
    }

    return (
        <Card sx={{width: '100%'}}>
            <CardHeader title={'Groups'}/>
            <CardContent>
                <Container>
                    <form onSubmit={handleSubmit(onSubmit)}
                          style={{display: 'flex', flexDirection: 'row', alignItems: 'center'}}>
                        <TextField {...register('groupName', {required: true})}/>
                        <Button type={"submit"}>Join Group</Button>
                    </form>
                </Container>
                <Box height={20}/>
                <List>
                    {
                        groups.map((group, index) =>
                            <ListItem key={index}>
                                <ListItemText>
                                    <Container sx={{display: 'flex', flexDirection: 'row', alignItems: 'center'}}>
                                        <Container>
                                            {group}
                                        </Container>
                                        <Container>
                                            <GroupMessageForm groupName={group} context={context} />
                                        </Container>
                                    </Container>
                                </ListItemText>
                                <ListItemButton sx={{color: 'red'}}>Leave</ListItemButton>
                            </ListItem>)
                    }
                </List>
            </CardContent>
        </Card>
    );
}

export default Groups;