using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

[InitializeOnLoad]
internal static class PlayerLimauMigration
{
    private const string ModelPath = "Assets/Resources/PlayerLimau.fbx";
    private const string PrefabPath = "Assets/Resources/Limau.prefab";
    private const string ControllerPath = "Assets/Scenes/Aset Game Scene/Animasi/Limau.controller";

    static PlayerLimauMigration()
    {
        EditorApplication.delayCall += Migrate;
    }

    private static void Migrate()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            return;

        try
        {
            ConfigureAnimationImport();
            AnimatorController controller = CreateController();
            UpdateLimauPrefab(controller);
            AssetDatabase.SaveAssets();
            File.WriteAllText(Path.GetFullPath("Temp/PlayerLimauMigration.txt"), "Migration completed.");
            Debug.Log("Limau prefab now uses PlayerLimau and its animation controller.");
        }
        catch (Exception exception)
        {
            Debug.LogException(exception);
        }
    }

    private static void ConfigureAnimationImport()
    {
        ModelImporter importer = AssetImporter.GetAtPath(ModelPath) as ModelImporter;
        if (importer == null)
            throw new InvalidOperationException($"Model importer not found: {ModelPath}");

        HashSet<string> loopingClips = new HashSet<string>
        {
            "Idle", "Walk", "Limau_Glow_Loop", "Limau_Inflate_Hold",
            "Limau_Silk_Hang", "Limau_Silk_Pull"
        };
        ModelImporterClipAnimation[] clips = importer.defaultClipAnimations
            .Where(clip => clip.lastFrame > clip.firstFrame)
            .ToArray();
        foreach (ModelImporterClipAnimation clip in clips)
        {
            string name = NamaKlip(clip.name);
            clip.loopTime = loopingClips.Contains(name);
            clip.loop = clip.loopTime;
        }

        importer.clipAnimations = clips;
        importer.SaveAndReimport();
    }

    private static AnimatorController CreateController()
    {
        if (AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath) != null)
            AssetDatabase.DeleteAsset(ControllerPath);

        Dictionary<string, AnimationClip> clips = AssetDatabase
            .LoadAllAssetsAtPath(ModelPath)
            .OfType<AnimationClip>()
            .Where(clip => !clip.name.StartsWith("__preview__", StringComparison.Ordinal))
            .GroupBy(clip => NamaKlip(clip.name))
            .ToDictionary(group => group.Key, group => group.First());

        AnimationClip Clip(string name)
        {
            if (!clips.TryGetValue(name, out AnimationClip clip))
                throw new InvalidOperationException($"Animation clip not found in {ModelPath}: {name}");
            return clip;
        }

        AnimatorController controller = AnimatorController.CreateAnimatorControllerAtPath(ControllerPath);
        string[] boolParameters = { "Grounded", "IsInflated", "IsGlowing", "IsSwinging", "IsPulling" };
        foreach (string parameter in boolParameters)
            controller.AddParameter(parameter, AnimatorControllerParameterType.Bool);
        controller.AddParameter("Speed", AnimatorControllerParameterType.Float);
        controller.AddParameter("AimY", AnimatorControllerParameterType.Float);
        string[] triggerParameters = { "InflateIn", "InflateOut", "GlowOn", "GlowOff", "InflateBounce", "SilkShoot" };
        foreach (string parameter in triggerParameters)
            controller.AddParameter(parameter, AnimatorControllerParameterType.Trigger);

        AnimatorControllerParameter[] parameters = controller.parameters;
        foreach (AnimatorControllerParameter parameter in parameters)
        {
            if (parameter.name == "Grounded")
                parameter.defaultBool = true;
        }
        controller.parameters = parameters;

        AnimatorStateMachine machine = controller.layers[0].stateMachine;
        foreach (ChildAnimatorState child in machine.states)
            machine.RemoveState(child.state);

        AnimatorState idle = AddState(machine, "Idle", Clip("Idle"));
        AnimatorState walk = AddState(machine, "Walk", Clip("Walk"));
        AnimatorState jump = AddState(machine, "Jump", Clip("Jump"));
        AnimatorState inflateIn = AddState(machine, "InflateIn", Clip("Limau_Inflate_In"));
        AnimatorState inflateHold = AddState(machine, "InflateHold", Clip("Limau_Inflate_Hold"));
        AnimatorState inflateOut = AddState(machine, "InflateOut", Clip("Limau_Inflate_Out"));
        AnimatorState inflateBounce = AddState(machine, "InflateBounce", Clip("Limau_Inflate_Bounce"));
        AnimatorState glowOn = AddState(machine, "GlowOn", Clip("Limau_Glow_On"));
        AnimatorState glowLoop = AddState(machine, "GlowLoop", Clip("Limau_Glow_Loop"));
        AnimatorState glowOff = AddState(machine, "GlowOff", Clip("Limau_Glow_Off"));
        AnimatorState silkShoot = AddState(machine, "SilkShoot", Clip("Limau_Silk_Shoot"));
        AnimatorState silkHang = AddState(machine, "SilkHang", Clip("Limau_Silk_Hang"));
        AnimatorState silkPull = AddState(machine, "SilkPull", Clip("Limau_Silk_Pull"));
        AnimatorState silkRelease = AddState(machine, "SilkRelease", Clip("Limau_Silk_Release"));
        machine.defaultState = idle;

        AddTransition(idle, walk, false, 0f, "Speed", AnimatorConditionMode.Greater, 0.1f);
        AddTransition(walk, idle, false, 0f, "Speed", AnimatorConditionMode.Less, 0.1f);
        AddTransition(idle, jump, false, 0f, "Grounded", AnimatorConditionMode.IfNot);
        AddTransition(walk, jump, false, 0f, "Grounded", AnimatorConditionMode.IfNot);
        AnimatorStateTransition jumpToIdle = AddTransition(
            jump, idle, false, 0f, "Grounded", AnimatorConditionMode.If);
        jumpToIdle.AddCondition(AnimatorConditionMode.Less, 0.1f, "Speed");
        AnimatorStateTransition jumpToWalk = AddTransition(
            jump, walk, false, 0f, "Grounded", AnimatorConditionMode.If);
        jumpToWalk.AddCondition(AnimatorConditionMode.Greater, 0.1f, "Speed");

        AddAnyStateTransition(machine, inflateIn, "InflateIn");
        AddAnyStateTransition(machine, inflateOut, "InflateOut");
        AddAnyStateTransition(machine, inflateBounce, "InflateBounce", "IsInflated");
        AddAnyStateTransition(machine, glowOn, "GlowOn");
        AddAnyStateTransition(machine, glowOff, "GlowOff");
        AddAnyStateTransition(machine, silkShoot, "SilkShoot");

        AddTransition(inflateIn, inflateHold, true, 0.9f, "IsInflated", AnimatorConditionMode.If);
        AddTransition(inflateIn, inflateOut, false, 0f, "IsInflated", AnimatorConditionMode.IfNot);
        AddTransition(inflateHold, inflateOut, false, 0f, "IsInflated", AnimatorConditionMode.IfNot);
        AddTransition(inflateBounce, inflateHold, true, 0.9f, "IsInflated", AnimatorConditionMode.If);
        AddTransition(inflateBounce, inflateOut, true, 0.9f, "IsInflated", AnimatorConditionMode.IfNot);
        AddTransition(glowOn, glowLoop, true, 0.9f, "IsGlowing", AnimatorConditionMode.If);
        AddTransition(glowOn, glowOff, false, 0f, "IsGlowing", AnimatorConditionMode.IfNot);
        AddTransition(glowLoop, glowOff, false, 0f, "IsGlowing", AnimatorConditionMode.IfNot);
        AddTransition(silkShoot, silkHang, true, 0.8f, "IsSwinging", AnimatorConditionMode.If);
        AddTransition(silkShoot, silkPull, true, 0.8f, "IsPulling", AnimatorConditionMode.If);
        AddTransition(silkHang, silkRelease, false, 0f, "IsSwinging", AnimatorConditionMode.IfNot);
        AddTransition(silkPull, silkRelease, false, 0f, "IsPulling", AnimatorConditionMode.IfNot);

        AddReturnTransitions(inflateOut, inflateHold, silkPull, silkHang, glowLoop, idle, walk, jump);
        AddReturnTransitions(glowOff, inflateHold, silkPull, silkHang, glowLoop, idle, walk, jump);
        AddReturnTransitions(silkShoot, inflateHold, silkPull, silkHang, glowLoop, idle, walk, jump, true);
        AddReturnTransitions(silkRelease, inflateHold, silkPull, silkHang, glowLoop, idle, walk, jump);

        AssetDatabase.SaveAssets();
        return controller;
    }

    private static AnimatorState AddState(AnimatorStateMachine machine, string name, Motion motion)
    {
        AnimatorState state = machine.AddState(name);
        state.motion = motion;
        return state;
    }

    private static void AddAnyStateTransition(
        AnimatorStateMachine machine,
        AnimatorState destination,
        string trigger,
        string boolParameter = null)
    {
        AnimatorStateTransition transition = machine.AddAnyStateTransition(destination);
        transition.hasExitTime = false;
        transition.duration = 0.1f;
        transition.canTransitionToSelf = false;
        transition.AddCondition(AnimatorConditionMode.If, 0f, trigger);
        if (boolParameter != null)
            transition.AddCondition(AnimatorConditionMode.If, 0f, boolParameter);
    }

    private static AnimatorStateTransition AddTransition(
        AnimatorState source,
        AnimatorState destination,
        bool hasExitTime,
        float exitTime,
        string parameter,
        AnimatorConditionMode mode,
        float threshold = 0f)
    {
        AnimatorStateTransition transition = source.AddTransition(destination);
        transition.hasExitTime = hasExitTime;
        transition.exitTime = exitTime;
        transition.duration = 0.1f;
        transition.AddCondition(mode, threshold, parameter);
        return transition;
    }

    private static void AddReturnTransitions(
        AnimatorState source,
        AnimatorState inflateHold,
        AnimatorState silkPull,
        AnimatorState silkHang,
        AnimatorState glowLoop,
        AnimatorState idle,
        AnimatorState walk,
        AnimatorState jump,
        bool waitForExit = false)
    {
        AddTransition(source, inflateHold, waitForExit, 0.95f, "IsInflated", AnimatorConditionMode.If);
        AddTransition(source, silkPull, waitForExit, 0.95f, "IsPulling", AnimatorConditionMode.If);
        AddTransition(source, silkHang, waitForExit, 0.95f, "IsSwinging", AnimatorConditionMode.If);
        AddTransition(source, glowLoop, waitForExit, 0.95f, "IsGlowing", AnimatorConditionMode.If);
        AddTransition(source, jump, waitForExit, 0.95f, "Grounded", AnimatorConditionMode.IfNot);

        AnimatorStateTransition toWalk = AddTransition(
            source, walk, waitForExit, 0.95f, "Grounded", AnimatorConditionMode.If);
        toWalk.AddCondition(AnimatorConditionMode.Greater, 0.1f, "Speed");
        AnimatorStateTransition toIdle = AddTransition(
            source, idle, waitForExit, 0.95f, "Grounded", AnimatorConditionMode.If);
        toIdle.AddCondition(AnimatorConditionMode.Less, 0.1f, "Speed");
    }

    private static string NamaKlip(string clipName)
    {
        int separator = clipName.LastIndexOf('|');
        return separator >= 0 ? clipName.Substring(separator + 1) : clipName;
    }

    private static void UpdateLimauPrefab(AnimatorController controller)
    {
        GameObject prefab = PrefabUtility.LoadPrefabContents(PrefabPath);
        try
        {
            Transform modelPivot = prefab.GetComponentsInChildren<Transform>(true)
                .FirstOrDefault(transform => transform.name == "ModelPivot");
            if (modelPivot == null)
                throw new InvalidOperationException("ModelPivot was not found in Limau.prefab.");

            for (int i = modelPivot.childCount - 1; i >= 0; i--)
                UnityEngine.Object.DestroyImmediate(modelPivot.GetChild(i).gameObject);

            GameObject modelAsset = AssetDatabase.LoadAssetAtPath<GameObject>(ModelPath);
            GameObject model = PrefabUtility.InstantiatePrefab(modelAsset, modelPivot) as GameObject;
            if (model == null)
                throw new InvalidOperationException($"Could not instantiate {ModelPath}.");

            model.transform.localPosition = Vector3.zero;
            model.transform.localRotation = Quaternion.Euler(0f, 90f, 0f);
            model.transform.localScale = Vector3.one;

            Renderer[] renderers = model.GetComponentsInChildren<Renderer>(true);
            if (renderers.Length == 0)
                throw new InvalidOperationException("PlayerLimau has no renderers.");

            Bounds bounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++)
                bounds.Encapsulate(renderers[i].bounds);

            Vector3 extents = modelPivot.InverseTransformVector(bounds.extents);
            float radius = Mathf.Max(extents.y, extents.z);
            Vector3 targetCenter = new Vector3(0f, radius, 0f);
            modelPivot.localPosition = targetCenter;
            Vector3 centerInPivot = modelPivot.InverseTransformPoint(bounds.center);
            model.transform.localPosition -= centerInPivot;

            CapsuleCollider capsule = prefab.GetComponent<CapsuleCollider>();
            if (capsule == null)
                throw new InvalidOperationException("CapsuleCollider was not found on Limau.");
            capsule.direction = 0;
            capsule.radius = radius;
            capsule.height = Mathf.Max(extents.x * 2f, radius * 2f);
            capsule.center = targetCenter;

            Animator animator = model.GetComponent<Animator>();
            if (animator == null)
                animator = model.AddComponent<Animator>();
            animator.runtimeAnimatorController = controller;
            animator.avatar = null;
            animator.applyRootMotion = false;
            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;

            Animator oldAnimator = modelPivot.GetComponent<Animator>();
            if (oldAnimator != null)
                oldAnimator.enabled = false;

            PlayerController playerController = prefab.GetComponent<PlayerController>();
            if (playerController == null)
                throw new InvalidOperationException("PlayerController was not found on Limau.");
            SerializedObject playerSerialized = new SerializedObject(playerController);
            playerSerialized.FindProperty("model").objectReferenceValue = modelPivot;
            playerSerialized.FindProperty("animator").objectReferenceValue = animator;
            playerSerialized.ApplyModifiedPropertiesWithoutUndo();

            LimauPhysicsHandler physics = prefab.GetComponent<LimauPhysicsHandler>();
            if (physics == null)
                throw new InvalidOperationException("LimauPhysicsHandler was not found on Limau.");
            SerializedObject physicsSerialized = new SerializedObject(physics);
            physicsSerialized.FindProperty("model").objectReferenceValue = modelPivot;
            physicsSerialized.ApplyModifiedPropertiesWithoutUndo();

            PrefabUtility.SaveAsPrefabAsset(prefab, PrefabPath);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(prefab);
        }
    }
}
