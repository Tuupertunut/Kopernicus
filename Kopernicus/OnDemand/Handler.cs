﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Kopernicus
{
    namespace OnDemand
    {
        public static class OnDemandStorage
        {
            public static Dictionary<ILoadOnDemand, bool> enabledMaps = new Dictionary<ILoadOnDemand,bool>(); // FIXME not really needed...
            public static Dictionary<string, List<ILoadOnDemand>> perBody = new Dictionary<string,List<ILoadOnDemand>>();
            public static string homeworldBody = "Kerbin";
            public static string currentBody = "";
            public static bool useOnDemand = false;

            public static void AddMap(string body, ILoadOnDemand map)
            {
                if (map == null)
                    return;

                if(!perBody.ContainsKey(body))
                    perBody[body] = new List<ILoadOnDemand>();

                perBody[body].Add(map);

            }
            public static void RemoveMap(string body, ILoadOnDemand map)
            {
                if (map == null)
                    return;

                if (perBody.ContainsKey(body))
                    perBody[body].Remove(map);

                enabledMaps.Remove(map);
            }

            public static void EnableMapList(List<ILoadOnDemand> maps, List<ILoadOnDemand> exclude = null)
            {
                if (exclude == null)
                    exclude = new List<ILoadOnDemand>();

                ILoadOnDemand curmap;
                for (int i = maps.Count - 1; i >= 0; --i)
                {
                    curmap = maps[i];
                    if(exclude.Contains(curmap))
                        continue;
                    curmap.Load();
                    enabledMaps[curmap] = true;
                }
            }
            public static void DisableMapList(List<ILoadOnDemand> maps, List<ILoadOnDemand> exclude = null)
            {
                if (exclude == null)
                    exclude = new List<ILoadOnDemand>();

                ILoadOnDemand curmap;
                for (int i = maps.Count - 1; i >= 0; --i)
                {
                    curmap = maps[i];
                    if(exclude.Contains(curmap))
                        continue;
                    curmap.Unload();
                    enabledMaps[curmap] = false;
                }
            }

            public static void EnableBody(string bname)
            {
                if (perBody.ContainsKey(bname))
                    EnableMapList(perBody[bname]);
            }
            public static void DisableBody(string bname)
            {
                if (perBody.ContainsKey(bname))
                {
                    DisableMapList(perBody[bname]);
                }
            }

            public static void EnableNot(List<string> bnames)
            {
                // FIXME if maps are shared, this may disable a map only to reenable it.
                // but we don't use this method, so...
                for (int i = bnames.Count - 1; i >= 0; --i)
                    DisableBody(bnames[i]);
                foreach (KeyValuePair<string, List<ILoadOnDemand>> kvp in perBody)
                {
                    if (bnames.Contains(kvp.Key))
                        continue;
                    EnableMapList(kvp.Value);
                }
            }
            public static void DisableNot(List<string> bnames)
            {
                List<ILoadOnDemand> finalList = new List<ILoadOnDemand>();
                List<ILoadOnDemand> curList;
                for (int i = bnames.Count - 1; i >= 0; --i)
                {
                    curList = perBody[bnames[i]];
                    for (int j = curList.Count - 1; j >= 0; --j)
                        if (!finalList.Contains(curList[j]))
                            finalList.Add(curList[j]);
                }
                foreach (KeyValuePair<string, List<ILoadOnDemand>> kvp in perBody)
                {
                    if (bnames.Contains(kvp.Key))
                        continue;

                    DisableMapList(kvp.Value, finalList);
                }
                EnableMapList(finalList);
            }
        }

        [KSPAddon(KSPAddon.Startup.EveryScene, false)]
        public class OnDemandHandler
        {
            protected static string FindHomeworld()
            {
                if (Planetarium.fetch != null)
                    return Planetarium.fetch.Home.bodyName;

                if (FlightGlobals.Bodies != null)
                {
                    for (int i = 0; i < FlightGlobals.Bodies.Count; ++i)
                        if (FlightGlobals.Bodies[i].isHomeWorld)
                            return FlightGlobals.Bodies[i].bodyName;
                }

                return "Kerbin";
            }

            public void Awake()
            {
                OnDemandStorage.homeworldBody = FindHomeworld();
                BodyUpdate();
            }

            public void Update()
            {
                BodyUpdate();
            }

            public void BodyUpdate()
            {
                // wait until Kopernicus is done loading
                if (!Templates.loadFinished)
                    return;

                List<string> bodies = new List<string>();
                if (!HighLogic.LoadedSceneIsFlight)
                {
                    bodies.Add(OnDemandStorage.homeworldBody);
                }
                if (FlightGlobals.currentMainBody != null && (HighLogic.LoadedSceneIsFlight || HighLogic.LoadedScene == GameScenes.LOADING || HighLogic.LoadedScene == GameScenes.LOADINGBUFFER))
                {
                    if (!bodies.Contains(FlightGlobals.currentMainBody.bodyName))
                        bodies.Add(FlightGlobals.currentMainBody.bodyName);
                }
                else if (FlightGlobals.ActiveVessel != null) // HOW!?
                {
                    if (FlightGlobals.ActiveVessel.mainBody != null)
                        if (!bodies.Contains(FlightGlobals.ActiveVessel.mainBody.bodyName))
                            bodies.Add(FlightGlobals.ActiveVessel.mainBody.bodyName);
                }

                OnDemandStorage.DisableNot(bodies);
            }
        }
    }
}
