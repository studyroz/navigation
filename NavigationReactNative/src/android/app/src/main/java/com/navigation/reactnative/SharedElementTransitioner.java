package com.navigation.reactnative;

import android.app.Activity;
import android.os.Build;
import android.transition.Transition;
import android.transition.TransitionInflater;
import android.transition.TransitionSet;

import java.util.HashMap;
import java.util.HashSet;

class SharedElementTransitioner {
    private Activity activity;
    private HashSet<String> sharedElements;
    private HashSet<String> loadedSharedElements = new HashSet<>();
    private HashMap<String, Transition> transitions = new HashMap<>();

    SharedElementTransitioner(Activity activity, HashSet<String> sharedElements) {
        this.activity = activity;
        this.sharedElements = sharedElements;
    }

    void load(String sharedElement, String transitionKey) {
        if (Build.VERSION.SDK_INT < Build.VERSION_CODES.LOLLIPOP)
            return;
        if (sharedElements.contains(sharedElement) && !loadedSharedElements.contains(sharedElement)) {
            loadedSharedElements.add(sharedElement);
            if (transitionKey == null)
                transitionKey = "move";
            Transition transition;
            if (transitions.containsKey(transitionKey))
                transition = transitions.get(transitionKey);
            else {
                String packageName = activity.getApplicationContext().getPackageName();
                int transitionId = activity.getResources().getIdentifier(transitionKey, "transition", packageName);
                if (transitionId == 0)
                    transitionId = activity.getResources().getIdentifier("move", "transition", packageName);
                transition = TransitionInflater.from(activity).inflateTransition(transitionId);
                transitions.put(transitionKey, transition);
            }
            transition.addTarget(sharedElement);
        }
        if(sharedElements.size() == loadedSharedElements.size()) {
            TransitionSet transitionSet = new TransitionSet();
            for(String key : transitions.keySet()) {
                transitionSet.addTransition(transitions.get(key));
            }
            activity.getWindow().setSharedElementEnterTransition(transitionSet);
            activity.startPostponedEnterTransition();
            if (activity instanceof SceneActivity)
                ((SceneActivity) activity).scene.transitioner = null;
            activity = null;
        }
    }
}
