﻿import LinkUtility = require('./LinkUtility');
import Navigation = require('navigation');
import ko = require('knockout');

var RefreshLink = ko.bindingHandlers['refreshLink'] = {
    init: (element, valueAccessor: () => any, allBindings: KnockoutAllBindingsAccessor, viewModel: any) => {
        LinkUtility.addListeners(element, () => setRefreshLink(element, valueAccessor, allBindings), allBindings, viewModel);
    },
    update: (element, valueAccessor, allBindings: KnockoutAllBindingsAccessor) => {
        setRefreshLink(element, valueAccessor, allBindings);
    }
};

function setRefreshLink(element: HTMLAnchorElement, valueAccessor: () => any, allBindings: KnockoutAllBindingsAccessor) {
    var data = {};
    var navigationData = ko.unwrap(valueAccessor());
    var stateNavigator: Navigation.StateNavigator = allBindings.get('stateNavigator');
    for (var key in navigationData) {
        data[key] = ko.unwrap(navigationData[key]);
    }
    LinkUtility.setLink(stateNavigator, element, () => stateNavigator.getRefreshLink(
        LinkUtility.getData(stateNavigator, data, ko.unwrap(allBindings.get('includeCurrentData')), ko.unwrap(allBindings.get('currentDataKeys'))))
    );
    LinkUtility.setActive(element, stateNavigator, data, allBindings);
}
export = RefreshLink;
