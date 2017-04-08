import { StateNavigator } from 'navigation';
import * as React from 'react';
import { BackAndroid } from 'react-native';

class NavigationBackAndroid extends React.Component {
    constructor(props, context) {
        super(props, context);
        this.state = {url: this.getStateNavigator().stateContext.url};
        this.onBack = () => {
            var stateNavigator = this.getStateNavigator();
            if (this.state.url === stateNavigator.stateContext.url) {
                var listener = this.props.navigating;
                var navigate = true;
                if (listener)
                    navigate = listener();
                var canNavigateBack = stateNavigator.canNavigateBack(1);
                if (navigate && canNavigateBack)
                    stateNavigator.navigateBack(1);
                return !navigate || canNavigateBack;
            }
            return false;
        }
    }
    static contextTypes = {
        stateNavigator: React.PropTypes.object
    }
    getStateNavigator() {
        return this.props.stateNavigator || this.context.stateNavigator;
    }
    componentDidMount() {
        BackAndroid.addEventListener('hardwareBackPress', this.onBack);
    }
    componentWillUnmount() {
        BackAndroid.removeEventListener('hardwareBackPress', this.onBack);
    }
    render() {
        return null;
    }
}

export default NavigationBackAndroid;
