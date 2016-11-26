import Animation from './Animation';
import Person from './Person';
import People from './People';
import { searchPeople, getPerson } from './Data';
import { StateNavigator } from 'navigation';
import React from 'react';
import ReactDOM from 'react-dom';

var stateNavigator = new StateNavigator([
    {key: 'people', route: '{pageNumber?}', defaults: {pageNumber: 1}},
    {key: 'person', route: 'person/{id}', defaults: {id: 0}, trackCrumbTrail: true}
]);

stateNavigator.states.people.navigated = function(data) {
    var people = searchPeople(data.pageNumber);
    ReactDOM.render(
        <Animation stateNavigator={stateNavigator} direction={-1}>
            <People people={people} stateNavigator={stateNavigator} />
        </Animation>,
        document.getElementById('content')
    );
}

stateNavigator.states.person.navigated = function(data) {
    var person = getPerson(data.id);
    ReactDOM.render(
        <Animation stateNavigator={stateNavigator} direction={1}>
            <Person person={person} stateNavigator={stateNavigator} />
        </Animation>,
        document.getElementById('content')
    );
}

stateNavigator.start();
