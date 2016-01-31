/**
 * Navigation v1.0.0
 * (c) Graham Mendick - http://grahammendick.github.io/navigation/example/cycle/navigation.html
 * License: Apache License 2.0
 */
(function(f){if(typeof exports==="object"&&typeof module!=="undefined"){module.exports=f()}else if(typeof define==="function"&&define.amd){define([],f)}else{var g;if(typeof window!=="undefined"){g=window}else if(typeof global!=="undefined"){g=global}else if(typeof self!=="undefined"){g=self}else{g=this}g.NavigationCycle = f()}})(function(){var define,module,exports;return (function e(t,n,r){function s(o,u){if(!n[o]){if(!t[o]){var a=typeof require=="function"&&require;if(!u&&a)return a(o,!0);if(i)return i(o,!0);var f=new Error("Cannot find module '"+o+"'");throw f.code="MODULE_NOT_FOUND",f}var l=n[o]={exports:{}};t[o][0].call(l.exports,function(e){var n=t[o][1][e];return s(n?n:e)},l,l.exports,e,t,n,r)}return n[o].exports}var i=typeof require=="function"&&require;for(var o=0;o<r.length;o++)s(r[o]);return s})({1:[function(_dereq_,module,exports){
(function (global){
/// <reference path="navigation.d.ts" />
/// <reference path="cycle-dom.d.ts" />
/// <reference path="rx.d.ts" />
var Navigation = (typeof window !== "undefined" ? window['Navigation'] : typeof global !== "undefined" ? global['Navigation'] : null);
var HistoryActionHook = (function () {
    function HistoryActionHook(historyAction) {
        this.historyAction = historyAction;
    }
    HistoryActionHook.prototype.hook = function (node) {
        node['historyAction'] = this.historyAction;
    };
    return HistoryActionHook;
})();
var LinkUtility = (function () {
    function LinkUtility() {
    }
    LinkUtility.getData = function (toData, includeCurrentData, currentDataKeys) {
        if (currentDataKeys)
            toData = Navigation.StateContext.includeCurrentData(toData, currentDataKeys.trim().split(/\s*,\s*/));
        if (includeCurrentData)
            toData = Navigation.StateContext.includeCurrentData(toData);
        return toData;
    };
    LinkUtility.isActive = function (key, val) {
        if (!Navigation.StateContext.state)
            return false;
        if (val != null) {
            var trackTypes = Navigation.StateContext.state.trackTypes;
            var currentVal = Navigation.StateContext.data[key];
            if (currentVal != null)
                return trackTypes ? val === currentVal : val.toString() == currentVal.toString();
            else
                return val === '';
        }
        return true;
    };
    LinkUtility.setActive = function (properties, active, activeCssClass, disableActive) {
        if (active && activeCssClass)
            properties.className = !properties.className ? activeCssClass : properties.className + ' ' + activeCssClass;
        if (active && disableActive)
            properties.href = null;
    };
    LinkUtility.setHistoryAction = function (properties, historyAction) {
        if (historyAction)
            properties.historyAction = new HistoryActionHook(historyAction);
    };
    LinkUtility.getHistoryAction = function (properties) {
        var historyAction = properties.historyAction;
        if (typeof historyAction === 'string')
            historyAction = Navigation.HistoryAction[historyAction];
        return historyAction;
    };
    return LinkUtility;
})();
module.exports = LinkUtility;
}).call(this,typeof global !== "undefined" ? global : typeof self !== "undefined" ? self : typeof window !== "undefined" ? window : {})
},{}],2:[function(_dereq_,module,exports){
(function (global){
var LinkUtility = _dereq_('./LinkUtility');
var Navigation = (typeof window !== "undefined" ? window['Navigation'] : typeof global !== "undefined" ? global['Navigation'] : null);
var CycleDOM = (typeof window !== "undefined" ? window['CycleDOM'] : typeof global !== "undefined" ? global['CycleDOM'] : null);
var NavigationBackLink = function (properties, children) {
    var newProperties = {};
    for (var key in properties)
        newProperties[key] = properties[key];
    var link = Navigation.StateController.getNavigationBackLink(properties.distance);
    newProperties.href = Navigation.settings.historyManager.getHref(link);
    LinkUtility.setHistoryAction(newProperties, properties.historyAction);
    return CycleDOM.h(newProperties.href ? 'a' : 'span', newProperties, children);
};
module.exports = NavigationBackLink;
}).call(this,typeof global !== "undefined" ? global : typeof self !== "undefined" ? self : typeof window !== "undefined" ? window : {})
},{"./LinkUtility":1}],3:[function(_dereq_,module,exports){
var NavigationDriver = _dereq_('./NavigationDriver');
var NavigationBackLink = _dereq_('./NavigationBackLink');
var NavigationLink = _dereq_('./NavigationLink');
var RefreshLink = _dereq_('./RefreshLink');
var NavigationCycle = (function () {
    function NavigationCycle() {
    }
    NavigationCycle.makeNavigationDriver = NavigationDriver;
    NavigationCycle.navigationBackLink = NavigationBackLink;
    NavigationCycle.navigationLink = NavigationLink;
    NavigationCycle.refreshLink = RefreshLink;
    return NavigationCycle;
})();
module.exports = NavigationCycle;
},{"./NavigationBackLink":2,"./NavigationDriver":4,"./NavigationLink":5,"./RefreshLink":6}],4:[function(_dereq_,module,exports){
(function (global){
var LinkUtility = _dereq_('./LinkUtility');
var Navigation = (typeof window !== "undefined" ? window['Navigation'] : typeof global !== "undefined" ? global['Navigation'] : null);
var Rx = (typeof window !== "undefined" ? window['Rx'] : typeof global !== "undefined" ? global['Rx'] : null);
function navigate(e) {
    var historyAction = LinkUtility.getHistoryAction(e);
    if (e.action)
        Navigation.StateController.navigate(e.action, e.toData, historyAction);
    if (!e.action && e.toData)
        Navigation.StateController.refresh(e.toData, historyAction);
    if (e.distance)
        Navigation.StateController.navigateBack(e.distance, historyAction);
    if (e.url)
        Navigation.StateController.navigateLink(e.url, false, historyAction);
}
function isolate(NavigationSource, key) {
    var navigated$ = NavigationSource.navigated
        .filter(function (context) { return context.state.parent.index + '-' + context.state.index === key; });
    return {
        navigated: navigated$
    };
}
var NavigationDriver = function (url) {
    return function (navigate$) {
        navigate$.subscribe(function (e) {
            if (!Navigation.StateContext.state)
                Navigation.start(url);
            if (e.target) {
                if (!e.ctrlKey && !e.shiftKey && !e.metaKey && !e.altKey && !e.button) {
                    e.preventDefault();
                    var link = Navigation.settings.historyManager.getUrl(e.target);
                    Navigation.StateController.navigateLink(link, false, LinkUtility.getHistoryAction(e.target));
                }
            }
            else {
                navigate(e);
            }
        });
        var navigated$ = new Rx.ReplaySubject(1);
        Navigation.StateController.onNavigate(function () { return navigated$.onNext(Navigation.StateContext); });
        return {
            navigated: navigated$,
            isolateSource: isolate
        };
    };
};
module.exports = NavigationDriver;
}).call(this,typeof global !== "undefined" ? global : typeof self !== "undefined" ? self : typeof window !== "undefined" ? window : {})
},{"./LinkUtility":1}],5:[function(_dereq_,module,exports){
(function (global){
var LinkUtility = _dereq_('./LinkUtility');
var Navigation = (typeof window !== "undefined" ? window['Navigation'] : typeof global !== "undefined" ? global['Navigation'] : null);
var CycleDOM = (typeof window !== "undefined" ? window['CycleDOM'] : typeof global !== "undefined" ? global['CycleDOM'] : null);
function isActive(action) {
    var nextState = Navigation.StateController.getNextState(action);
    return nextState === nextState.parent.initial && nextState.parent === Navigation.StateContext.dialog;
}
var NavigationLink = function (properties, children) {
    var newProperties = {};
    for (var key in properties)
        newProperties[key] = properties[key];
    var active = true;
    for (var key in properties.toData) {
        active = active && LinkUtility.isActive(key, properties.toData[key]);
    }
    var toData = LinkUtility.getData(properties.toData, properties.includeCurrentData, properties.currentDataKeys);
    var link = Navigation.StateController.getNavigationLink(properties.action, properties.toData);
    newProperties.href = Navigation.settings.historyManager.getHref(link);
    active = active && !!newProperties.href && isActive(properties.action);
    LinkUtility.setActive(newProperties, active, properties.activeCssClass, properties.disableActive);
    LinkUtility.setHistoryAction(newProperties, properties.historyAction);
    return CycleDOM.h(newProperties.href ? 'a' : 'span', newProperties, children);
};
module.exports = NavigationLink;
}).call(this,typeof global !== "undefined" ? global : typeof self !== "undefined" ? self : typeof window !== "undefined" ? window : {})
},{"./LinkUtility":1}],6:[function(_dereq_,module,exports){
(function (global){
var LinkUtility = _dereq_('./LinkUtility');
var Navigation = (typeof window !== "undefined" ? window['Navigation'] : typeof global !== "undefined" ? global['Navigation'] : null);
var CycleDOM = (typeof window !== "undefined" ? window['CycleDOM'] : typeof global !== "undefined" ? global['CycleDOM'] : null);
var RefreshLink = function (properties, children) {
    var newProperties = {};
    for (var key in properties)
        newProperties[key] = properties[key];
    var active = true;
    for (var key in properties.toData) {
        active = active && LinkUtility.isActive(key, properties.toData[key]);
    }
    var toData = LinkUtility.getData(properties.toData, properties.includeCurrentData, properties.currentDataKeys);
    var link = Navigation.StateController.getRefreshLink(toData);
    newProperties.href = Navigation.settings.historyManager.getHref(link);
    active = active && !!newProperties.href;
    LinkUtility.setActive(newProperties, active, properties.activeCssClass, properties.disableActive);
    LinkUtility.setHistoryAction(newProperties, properties.historyAction);
    return CycleDOM.h(newProperties.href ? 'a' : 'span', newProperties, children);
};
module.exports = RefreshLink;
}).call(this,typeof global !== "undefined" ? global : typeof self !== "undefined" ? self : typeof window !== "undefined" ? window : {})
},{"./LinkUtility":1}]},{},[3])(3)
});