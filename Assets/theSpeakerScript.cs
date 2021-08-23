using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using KModkit;
using Rnd = UnityEngine.Random;
using System.Text.RegularExpressions;

public class theSpeakerScript : MonoBehaviour {

    //public stuff
    public KMAudio Audio;
    public List<KMSelectable> Buttons;
    public List<MeshRenderer> Plugs;
    public List<MeshRenderer> Leds;
    public List<Transform> Sliders;
    public MeshRenderer Speaker;
    public TextMesh SN;
    public TextMesh CBText;
    public KMColorblindMode CBM;
    public KMBombModule Module;

    //private stuff
    private readonly List<List<int>> order = new List<List<int>> { new List<int> { 0, 1, 7, 2, 6, 3, 5, 4 }, new List<int> { 1, 2, 0, 3, 7, 4, 6, 5 }, new List<int> { 5, 6, 4, 7, 3, 0, 2, 1 }, new List<int> { 3, 4, 2, 5, 1, 6, 0, 7 }, new List<int> { 4, 5, 3, 6, 2, 7, 1, 0 }, new List<int> { 2, 3, 1, 4, 0, 5, 7, 6 }, new List<int> { 7, 0, 6, 1, 5, 2, 4, 3 }, new List<int> { 6, 7, 5, 0, 4, 1, 3, 2 } };
    private List<bool> hl = new List<bool> { };
    private int done = 0;
    private List<int> solution;
    private List<int> portcolours;
    private List<int> sliderpos;
    private string serial;
    private bool CBA;
    private bool solved;

    //logging
    static int _moduleIdCounter = 1;
    int _moduleID = 0;

    void Awake()
    {
        _moduleID = _moduleIdCounter++;
        //kbgcrmyw
        portcolours = Enumerable.Range(0, 8).ToList().Shuffle().Concat(new List<int> { Rnd.Range(0, 8) }).ToList();
        for (int i = 0; i < 8; i++)
        {
            Buttons[i].GetComponent<MeshRenderer>().material.color = new Color(0.25f + (portcolours[i] / 4) * 0.5f, 0.25f + ((portcolours[i] / 2) % 2) * 0.5f, 0.25f + (portcolours[i] % 2) * 0.5f);
            Plugs[i].enabled = false;
            hl.Add(false);
            int x = i;
            Buttons[i].OnHighlight += delegate { hl[x] = true; };
            Buttons[i].OnHighlightEnded += delegate { hl[x] = false; };
            Buttons[i].OnInteract += delegate
            {
                if (!solved)
                {
                    Buttons[x].AddInteractionPunch(1f);
                    if (x == solution[done])
                    {
                        Plugs[x].enabled = true;
                        Audio.PlaySoundAtTransform("Plug", Speaker.transform);
                        Debug.LogFormat("[The Speaker #{0}] You pressed position {1}, which is correct.", _moduleID, (x + 1));
                        done++;
                        if (done == 8)
                        {
                            Leds[x].material.color = new Color(0, 1, 0);
                            Module.HandlePass();
                            Debug.LogFormat("[The Speaker #{0}] Module Solved!", _moduleID);
                            solved = true;
                            Audio.PlaySoundAtTransform("Solve", Speaker.transform);
                        }
                    }
                    else if (!solution.Take(done).Contains(x))
                    {
                        Debug.LogFormat("[The Speaker #{0}] You pressed position {1} where I expected {2}. Strike!", _moduleID, (x + 1), (solution[done] + 1));
                        Module.HandleStrike();
                    }
                }
                return false;
            };
        }
        Speaker.material.color = new Color(0.1875f + (portcolours[8] / 4) * 0.375f, 0.1875f + ((portcolours[8] / 2) % 2) * 0.375f, 0.1875f + (portcolours[8] % 2) * 0.375f);
        sliderpos = new List<int> { Rnd.Range(0, 3), Rnd.Range(0, 3), Rnd.Range(0, 3) };
        for (int i = 0; i < 3; i++)
            Sliders[i].localPosition = new Vector3(0, 3, 0.25f * (sliderpos[i] - 1));
        serial = String.Empty;
        for (int i = 0; i < 2; i++)
            serial += "ABCDEFGHIJKLMNOPQRSTUVWXYZ"[Rnd.Range(0, 26)];
        for (int i = 0; i < 2; i++)
            serial += "0123456789"[Rnd.Range(0, 10)];
        SN.text = serial;
        GenerateSolution();
    }

    void Start () {
        CBA = CBM.ColorblindModeActive;
        CBText.text = "KBGCRMYW"[portcolours[8]].ToString() + "\n" + portcolours.Take(4).Select(x => "KBGCRMYW"[x]).Join("") + "\n" + portcolours.Skip(4).Take(4).Select(x => "KBGCRMYW"[x]).Join("");
    }
    
    void Update () {
        if (!solved)
        {
            CBText.GetComponent<Renderer>().enabled = CBA;
            for (int i = 0; i < 8; i++)
            {
                if (solution.IndexOf(i) < done)
                    Leds[i].material.color = new Color(0, 1, 0);
                else if (hl[i])
                    Leds[i].material.color = new Color(1, 0.25f, 0);
                else
                    Leds[i].material.color = new Color(0, 0, 0);
            }
        }
        else
            CBText.GetComponent<Renderer>().enabled = false;
    }

    private void GenerateSolution()
    {
        List<int> set = order[portcolours[0]];
        if (serial[2] == '0' && serial[3] == '0' && sliderpos.Sum() == 0)
            solution = set.Select(x => portcolours.IndexOf(x)).ToList();
        else
        {
            solution = new List<int> { };
            for (int i = 0; i < 8; i++)
                switch (set[i])
                {
                    case 0:
                        int k = portcolours[7];
                        bool kgood = true;
                        int factor = 4;
                        for (int j = 0; j < 3; j++)
                        {
                            if ((((k / factor) % 2) * 2 - sliderpos[j]) / 2 != 0)
                                kgood = false;
                            factor /= 2;
                        }
                        if (kgood)
                        {
                            for (int j = 0; j < 8 && kgood; j++)
                                if (!solution.Contains(7 - j))
                                {
                                    solution.Add(7 - j);
                                    kgood = false;
                                }
                        }
                        else
                        {
                            kgood = true;
                            for (int j = 0; j < 8 && kgood; j++)
                                if (!solution.Contains(j))
                                {
                                    solution.Add(j);
                                    kgood = false;
                                }
                        }
                        break;
                    case 1:
                        int b = (serial[1] - serial[0] > 0 ? 4 : 0) + ((serial[0] - 'A') % 2) * 2 + ((serial[1] - 'A') % 2);
                        List<int> bpos = new List<int> { 0, 1, 2, 3, 7, 6, 5, 4 };
                        b = bpos.IndexOf(b);
                        bool bgood = true;
                        for (int j = 0; j < 8 && bgood; j++)
                            if (!solution.Contains(bpos[(b + j) % 8]))
                            {
                                solution.Add(bpos[(b + j) % 8]);
                                bgood = false;
                            }
                        break;
                    case 2:
                        int g = portcolours[8];
                        int ga = portcolours.IndexOf(2) + 1;
                        int gb = i + 1;
                        int gc = (serial[2] - '0') + (serial[3] - '0');
                        int g2 = 0;
                        switch (g)
                        {
                            case 0:
                                g2 = (ga + gb + gc + 7) % 8;
                                break;
                            case 1:
                                g2 = (ga + gb * gc + 7) % 8;
                                break;
                            case 2:
                                g2 = (ga * gb + gc + 7) % 8;
                                break;
                            case 3:
                                g2 = (ga * gb * gc + 7) % 8;
                                break;
                            case 4:
                                g2 = (ga + gb + gc + 3) % 8;
                                break;
                            case 5:
                                g2 = (ga + gb * gc + 3) % 8;
                                break;
                            case 6:
                                g2 = (ga * gb + gc + 3) % 8;
                                break;
                            case 7:
                                g2 = (ga * gb * gc + 3) % 8;
                                break;
                        }
                        bool ggood = true;
                        for (int j = 0; j < 8 && ggood; j++)
                            if (!solution.Contains((g2 + j * 3) % 8))
                            {
                                solution.Add((g2 + j * 3) % 8);
                                ggood = false;
                            }
                        break;
                    case 3:
                        int c = sliderpos.Sum() % (8 - i);
                        for (int j = 0; j < 8; j++)
                            if (!solution.Contains(j))
                            {
                                if (c == 0)
                                    solution.Add(j);
                                c--;
                            }
                        break;
                    case 4:
                        int ra = portcolours.IndexOf(4);
                        int rb = portcolours.IndexOf(portcolours[8]);
                        int r = new int[] { (ra % 4 - rb % 4), (rb % 4 - ra % 4) }.Max() + (ra / 4 + rb / 4) % 2;
                        bool rgood = true;
                        for (int j = 0; j < 8 && rgood; j++)
                            if (!solution.Contains((r + j) % 8))
                            {
                                solution.Add((r + j) % 8);
                                rgood = false;
                            }
                        break;
                    case 5:
                        int m = i;
                        bool mgood = true;
                        for (int j = 0; j < 8 && mgood; j++)
                            if (!solution.Contains((m + 8 - j) % 8))
                            {
                                solution.Add((m + 8 - j) % 8);
                                mgood = false;
                            }
                        break;
                    case 6:
                        int y = set.IndexOf(portcolours[(serial[0] - 'A') % 9]);
                        bool ygood = true;
                        for (int j = 0; j < 8 && ygood; j++)
                            if (!solution.Contains(portcolours.IndexOf(set[(y + j) % 8])))
                            {
                                solution.Add(portcolours.IndexOf(set[(y + j) % 8]));
                                ygood = false;
                            }
                        break;
                    case 7:
                        int w = 0;
                        if (i == 0)
                            w = portcolours.IndexOf(portcolours[8]);
                        else
                            w = solution.Last();
                        bool wgood = true;
                        if (portcolours.IndexOf(7) > w)
                        {
                            for (int j = 0; j < 8 && wgood; j++)
                                if (!solution.Contains((w + j) % 8))
                                {
                                    solution.Add((w + j) % 8);
                                    wgood = false;
                                }
                        }
                        else if (portcolours.IndexOf(7) < w)
                        {
                            for (int j = 0; j < 8 && wgood; j++)
                                if (!solution.Contains((w + 8 - j) % 8))
                                {
                                    solution.Add((w + 8 - j) % 8);
                                    wgood = false;
                                }
                        }
                        else
                        {
                            for (int j = 0; j < 8 && wgood; j++)
                                if (!solution.Contains((w + j * 5) % 8))
                                {
                                    solution.Add((w + j * 5) % 8);
                                    wgood = false;
                                }
                        }
                        break;
                }
        }
        Debug.LogFormat("[The Speaker #{0}] The colours in order are: {1}.", _moduleID, portcolours.Take(8).Select(x => "KBGCRMYW"[x]).Join(""));
        Debug.LogFormat("[The Speaker #{0}] The speaker colour is: {1}.", _moduleID, "KBGCRMYW"[portcolours[8]]);
        Debug.LogFormat("[The Speaker #{0}] The slider positions are: {1}.", _moduleID, sliderpos.Select(x => "dmu"[x]).Join(""));
        Debug.LogFormat("[The Speaker #{0}] The speaker serial is: {1}.", _moduleID, serial);
        Debug.LogFormat("[The Speaker #{0}] Expected presses are: {1}.", _moduleID, solution.Select(x => x + 1).Join(", "));
        for (int i = 0; i < 8; i++)
            Plugs[i].GetComponent<MeshRenderer>().material.color = new Color(0.25f + (set[solution.IndexOf(i)] / 4) * 0.5f, 0.25f + ((set[solution.IndexOf(i)] / 2) % 2) * 0.5f, 0.25f + (set[solution.IndexOf(i)] % 2) * 0.5f);
    }

#pragma warning disable 414
    private string TwitchHelpMessage = "'!{0} press 1 2 3 4 5 6 7 8' to press those positions (ordered in reading order). '!{0} colorblind' to toggle colorblind mode.";
#pragma warning restore 414
    IEnumerator ProcessTwitchCommand(string command)
    {
        yield return null;
        command = command.ToLowerInvariant();
        if (command == "colorblind")
        {
            CBA = !CBA;
        }
        else
        {
            if (Regex.IsMatch(command, @"^press\s((1|2|3|4|5|6|7|8)(\s?))+$"))
            {
                MatchCollection matches = Regex.Matches(command.Replace("press", ""), @"(1|2|3|4|5|6|7|8)");
                foreach (Match match in matches)
                    foreach (Capture capture in match.Captures)
                    {
                        Debug.Log(capture.ToString());
                        Buttons[capture.ToString()[0] - '1'].OnInteract();
                        yield return null;
                    }
                yield return "solve";
            }
            else
                yield return "sendtochaterror Invalid command.";
        }
    }

    IEnumerator TwitchHandleForcedSolve()
    {
        yield return true;
        while (done < 8)
        {
            Buttons[solution[done]].OnInteract();
            yield return true;
        }
    }
}
