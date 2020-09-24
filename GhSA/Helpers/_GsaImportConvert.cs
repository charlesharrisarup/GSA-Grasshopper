﻿using System;
using System.Linq;
using System.Drawing;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using GsaAPI;
using Rhino.Geometry;
using Grasshopper;
using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Types;
using GhSA.Parameters;

namespace GhSA.Util.Gsa
{
    public class GsaImport
    {
        public static List<GsaNode> GsaGetPoint(Model model, string nodeList)
        {
            // Create empty Gsa Node to work on:
            Node node = new Node();
            GsaNode n = new GsaNode();
            List<GsaNode> nodes = new List<GsaNode>();

            // Create dictionary to read list of nodes:
            IReadOnlyDictionary<int, Node> nDict;
            nDict = model.Nodes(nodeList);

            // Loop through all nodes in Node dictionary and add points to Rhino point list
            for (int i = 0; i < nDict.Keys.Max(); i++)
            {
                if (nDict.TryGetValue(i+1, out node)) //1-base numbering
                {
                    var p = node.Position;
                    n = new GsaNode(new Point3d(p.X, p.Y, p.Z), i+1);
                    n.node = node;
                    if(node.SpringProperty > 0)
                    {
                        //GsaSpring spring = new GsaSpring();
                        //sDict = model. //
                        //spring.X = 
                        //n.Spring = spring;
                    }
                    nodes.Add(n.Duplicate());
                }
                else
                    nodes.Add(null);
            }
            //n.node.Dispose();
            //node.Dispose();
            return nodes;
        }
        public static Tuple<DataTree<GsaElement1dGoo>, DataTree<GsaElement2dGoo>> GsaGetElem(Model model, string elemList = "all", bool join = true)
        {
            // Create empty GsaAPI Element to work on:
            Element elem = new Element();
            Node node = new Node();

            // Create GhSA elements to work on 
            GsaElement1d elem1d = new GsaElement1d();
            GsaElement2d elem2d = new GsaElement2d();

            // Create dictionaries to read list of elements and nodes:
            IReadOnlyDictionary<int, Element> eDict;
            eDict = model.Elements(elemList);
            IReadOnlyDictionary<int, Node> nDict;
            nDict = model.Nodes("all");

            // Create lists for Rhino lines and meshes
            DataTree<GsaElement1dGoo> elem1ds = new DataTree<GsaElement1dGoo>();
            DataTree<GsaElement2dGoo> elem2ds = new DataTree<GsaElement2dGoo>();
            DataTree<Element> elements = new DataTree<Element>();
            DataTree<Mesh> meshes = new DataTree<Mesh>();
            List<Point3d> pts = new List<Point3d>();
            Mesh tempMesh = new Mesh();
            Line line = new Line();
            LineCurve ln = new LineCurve();

            GH_Path path = new GH_Path();
            List<int> meshPaths = new List<int>();

            if (!join)
            {
                elem1ds.EnsurePath(0);
                elem2ds.EnsurePath(0);
                int max = eDict.Count;
                if (max > 0)
                {
                    for (int i = 0; i < eDict.Keys.ElementAt(max - 1); i++)
                    {
                        elem1ds.Branches[0].Add(null);
                        elem2ds.Branches[0].Add(null);
                    }
                }
            }

            // Loop through all nodes in Node dictionary and add points to Rhino point list
            foreach (var key in eDict.Keys)
            {
                if (eDict.TryGetValue(key, out elem))
                {
                    List<int> topo = elem.Topology.ToList();
                    int prop = 0;
                    if (join)
                        prop = elem.Property - 1; // actually branch not property
                    

                    // Beams (1D elements):
                    if (topo.Count == 2)
                    {
                        for (int i = 0; i <= 1; i++)
                        {
                            if (nDict.TryGetValue(topo[i], out node))
                            {
                                {
                                    var p = node.Position;
                                    pts.Add(new Point3d(p.X, p.Y, p.Z));
                                }
                            }
                        }
                        line = new Line(pts[0], pts[1]);
                        ln = new LineCurve(line);
                        elem1d = new GsaElement1d(ln);
                        elem1d.Element = elem;
                        GsaBool6 start = new GsaBool6();
                        start.X = elem.Release(0).X;
                        start.Y = elem.Release(0).Y;
                        start.Z = elem.Release(0).Z;
                        start.XX = elem.Release(0).XX;
                        start.YY = elem.Release(0).YY;
                        start.ZZ = elem.Release(0).ZZ;
                        elem1d.ReleaseStart = start;
                        GsaBool6 end = new GsaBool6();
                        end.X = elem.Release(1).X;
                        end.Y = elem.Release(1).Y;
                        end.Z = elem.Release(1).Z;
                        end.XX = elem.Release(1).XX;
                        end.YY = elem.Release(1).YY;
                        end.ZZ = elem.Release(1).ZZ;
                        elem1d.ReleaseEnd = end;
                        elem1d.ID = key;

                        pts.Clear();
                        elem1ds.EnsurePath(prop);
                        path = new GH_Path(prop);
                        if (join)
                            elem1ds.Add(new GsaElement1dGoo(elem1d.Duplicate()), path);
                        else
                            elem1ds[path, key - 1] = new GsaElement1dGoo(elem1d.Duplicate());
                    }

                    // Shells (2D elements)
                    if (topo.Count > 2) // & topo.Count < 5)
                    {
                        tempMesh = new Mesh();
                        // Get verticies:
                        for (int i = 0; i < topo.Count; i++)
                        {
                            if (nDict.TryGetValue(topo[i], out node))
                            {
                                {
                                    var p = node.Position;
                                    tempMesh.Vertices.Add(new Point3d(p.X, p.Y, p.Z));
                                }
                            }
                        }

                        // Create mesh face (Tri- or Quad):
                        if (topo.Count == 3)
                            tempMesh.Faces.AddFace(0, 1, 2);
                        if (topo.Count == 4)
                            tempMesh.Faces.AddFace(0, 1, 2, 3);
                        else
                        {
                            //it must be a TRI6 or a QUAD8
                            List<Point3f> tempPts = tempMesh.Vertices.ToList();
                            double x = 0; double y = 0; double z = 0;
                            for (int i = 0; i < tempPts.Count; i++)
                            {
                                x = x + tempPts[i].X; y = y + tempPts[i].Y; z = z + tempPts[i].Z;
                            }
                            x = x / tempPts.Count; y = y / tempPts.Count; z = z / tempPts.Count;
                            tempMesh.Vertices.Add(new Point3d(x, y, z));

                            if (topo.Count == 6)
                            {
                                tempMesh.Faces.AddFace(0, 3, 6);
                                tempMesh.Faces.AddFace(3, 1, 6);
                                tempMesh.Faces.AddFace(1, 4, 6);
                                tempMesh.Faces.AddFace(4, 2, 6);
                                tempMesh.Faces.AddFace(2, 5, 6);
                                tempMesh.Faces.AddFace(5, 0, 6);
                            }

                            if (topo.Count == 8)
                            {
                                tempMesh.Faces.AddFace(0, 4, 8, 7);
                                tempMesh.Faces.AddFace(1, 5, 8, 4);
                                tempMesh.Faces.AddFace(2, 6, 8, 5);
                                tempMesh.Faces.AddFace(3, 7, 8, 6);
                            }
                        }
                        List<int> ids = new List<int>();
                        ids.Add(key);
                        elem2d.ID = ids;
                        if (join)
                        {
                            meshes.EnsurePath(prop);
                            elements.EnsurePath(prop);
                            path = new GH_Path(prop);

                            meshes.Add(tempMesh, path);
                            elements.Add(elem, path);
                        }
                        else
                        {
                            elem2d = new GsaElement2d(tempMesh);
                            List<Element> elemProps = new List<Element>();
                            elemProps.Add(elem);
                            elem2d.Elements = elemProps;
                            elem2ds[path, key - 1] = new GsaElement2dGoo(elem2d.Duplicate());
                            //elem2ds.Add(new GsaElement2dGoo(elem2d.Duplicate()));
                        }
                    }
                }
            }

            if (join)
            {
                foreach (GH_Path ipath in meshes.Paths)
                {
                    //##### Join meshes #####

                    //List of meshes in each branch
                    List<Mesh> mList = meshes.Branch(ipath);

                    //new temp mesh
                    Mesh m = new Mesh();
                    //Append list of meshes (faster than appending each mesh one by one)
                    m.Append(mList);

                    //split mesh into connected pieces
                    Mesh[] meshy = m.SplitDisjointPieces();

                    //clear whatever is in the current branch (the list in mList)
                    meshes.Branch(ipath).Clear();
                    //rewrite new joined and split meshes to new list in same path:
                    for (int j = 0; j < meshy.Count(); j++)
                        meshes.Add(meshy[j], ipath);
                }
                foreach (GH_Path ipath in meshes.Paths)
                {
                    List<Mesh> mList = meshes.Branch(ipath);
                    foreach (Mesh mesh in mList)
                    {
                        elem2d = new GsaElement2d(mesh);
                        List<Element> elemProps = new List<Element>();
                        for (int i = 0; i < mesh.Faces.Count(); i++)
                            elemProps.Add(elements[ipath, 0]);
                        elem2d.Elements = elemProps;
                        elem2ds.Add(new GsaElement2dGoo(elem2d.Duplicate()));
                    }
                }
            }
            //elem1d.Element.Dispose();
            //elem2d.Elements.Clear();
            //elem.Dispose();
            //node.Dispose();

            return new Tuple<DataTree<GsaElement1dGoo>, DataTree<GsaElement2dGoo>>(elem1ds, elem2ds);
        }

        public static Tuple<DataTree<GsaMember1dGoo>, DataTree<GsaMember2dGoo>> GsaGetMemb(Model model, string memList = "all", bool propGraft = true)
        {
            // Create empty GsaAPI Element to work on:
            Member mem = new Member();
            Node node = new Node();

            // Create GhSA elements to work on 
            GsaMember1d mem1d = new GsaMember1d();
            GsaMember2d mem2d = new GsaMember2d();

            // Create dictionaries to read list of elements and nodes:
            IReadOnlyDictionary<int, Member> mDict;
            mDict = model.Members(memList);
            IReadOnlyDictionary<int, Node> nDict;
            nDict = model.Nodes("all");

            // Create lists for Rhino lines and meshes
            DataTree<GsaMember1dGoo> mem1ds = new DataTree<GsaMember1dGoo>();
            DataTree<GsaMember2dGoo> mem2ds = new DataTree<GsaMember2dGoo>();

            List<List<Point3d>> void_topo = new List<List<Point3d>>(); //list of lists of void points /member2d
            List<List<string>> void_topoType = new List<List<string>>(); ////list of polyline curve type (arch or line) for void /member2d
            List<List<Point3d>> incLines_topo = new List<List<Point3d>>(); //list of lists of line inclusion topology points /member2d
            List<List<string>> inclLines_topoType = new List<List<string>>(); ////list of polyline curve type (arch or line) for inclusion /member2d
            List<Point3d> incl_pts = new List<Point3d>(); //list of points for inclusion /member2d

            PolyCurve m_crv = new PolyCurve(); //Polyline for visualisation /member1d/member2d
            List<Point3d> topopts = new List<Point3d>(); // list of topology points for visualisation /member1d/member2d
            List<string> topoType = new List<string>(); //list of polyline curve type (arch or line) for member1d/2d

            GH_Path path = new GH_Path();

            if (!propGraft)
            {
                mem1ds.EnsurePath(0);
                mem2ds.EnsurePath(0);
                int max = mDict.Count;
                if (max > 0)
                {
                    for (int i = 0; i < mDict.Keys.ElementAt(max - 1); i++)
                    {
                        mem1ds.Branches[0].Add(null);
                        mem2ds.Branches[0].Add(null);
                    }
                }
            }

            // Loop through all nodes in Node dictionary and add points to Rhino point list
            foreach (var key in mDict.Keys)
            {
                if (mDict.TryGetValue(key, out mem))
                {
                    int prop = 0;
                    if (propGraft) 
                        prop = mem.Property - 1;
                    
                    // Build topology lists
                    string toporg = mem.Topology; //original topology list

                    Tuple<Tuple<List<int>, List<string>>, Tuple<List<List<int>>, List<List<string>>>,
                        Tuple<List<List<int>>, List<List<string>>>, List<int>> topologyTuple = topology_detangler(toporg);
                    Tuple<List<int>, List<string>> topoTuple = topologyTuple.Item1;
                    Tuple<List<List<int>>, List<List<string>>> voidTuple = topologyTuple.Item2;
                    Tuple<List<List<int>>, List<List<string>>> lineTuple = topologyTuple.Item3;

                    List<int> topo_int = topoTuple.Item1;
                    topoType = topoTuple.Item2;

                    List<List<int>> void_topo_int = voidTuple.Item1;
                    void_topoType = voidTuple.Item2;

                    List<List<int>> incLines_topo_int = lineTuple.Item1;
                    inclLines_topoType = lineTuple.Item2;

                    List<int> inclpts = topologyTuple.Item4;

                    // replace topology integers with actual points
                    for (int i = 0; i < topo_int.Count; i++)
                    {
                        if (nDict.TryGetValue(topo_int[i], out node))
                        {
                            var p = node.Position;
                            topopts.Add(new Point3d(p.X, p.Y, p.Z));
                        }
                    }

                    for (int i = 0; i < void_topo_int.Count; i++)
                    {
                        void_topo.Add(new List<Point3d>());
                        for (int j = 0; j < void_topo_int[i].Count; j++)
                        {
                            if (nDict.TryGetValue(void_topo_int[i][j], out node))
                            {
                                var p = node.Position;
                                void_topo[i].Add(new Point3d(p.X, p.Y, p.Z));
                            }
                        }
                    }

                    for (int i = 0; i < incLines_topo_int.Count; i++)
                    {
                        incLines_topo.Add(new List<Point3d>());
                        for (int j = 0; j < incLines_topo_int[i].Count; j++)
                        {
                            if (nDict.TryGetValue(incLines_topo_int[i][j], out node))
                            {
                                var p = node.Position;
                                incLines_topo[i].Add(new Point3d(p.X, p.Y, p.Z));
                            }
                        }
                    }

                    for (int i = 0; i < inclpts.Count; i++)
                    {
                        if (nDict.TryGetValue(inclpts[i], out node))
                        {
                            var p = node.Position;
                            incl_pts.Add(new Point3d(p.X, p.Y, p.Z));
                        }
                    }

                    if (mem.Type == MemberType.GENERIC_1D | mem.Type == MemberType.BEAM | mem.Type == MemberType.CANTILEVER |
                        mem.Type == MemberType.COLUMN | mem.Type == MemberType.COMPOS | mem.Type == MemberType.PILE)
                    {
                        mem1d = new GsaMember1d(topopts, topoType);
                        mem1d.ID = key;
                        mem1d.member = mem;
                        mem1ds.EnsurePath(prop);
                        path = new GH_Path(prop);
                        if (propGraft)
                            mem1ds.Add(new GsaMember1dGoo(mem1d.Duplicate()), path);
                        else
                            mem1ds[path, key-1] = new GsaMember1dGoo(mem1d.Duplicate());
                    }
                    else
                    {
                        mem2d = new GsaMember2d(topopts, topoType, void_topo, void_topoType, incLines_topo, inclLines_topoType, incl_pts);
                        mem2d.member = mem;
                        mem2d.ID = key;
                        mem2ds.EnsurePath(prop);
                        path = new GH_Path(prop);
                        if (propGraft)
                            mem2ds.Add(new GsaMember2dGoo(mem2d.Duplicate()), path);
                        else
                            mem2ds[path, key-1] = new GsaMember2dGoo(mem2d.Duplicate());
                    }

                    topopts.Clear();
                    topoType.Clear();
                    void_topo.Clear();
                    void_topoType.Clear();
                    incLines_topo.Clear();
                    inclLines_topoType.Clear();
                    incl_pts.Clear();
                    
                }
            }

            return new Tuple<DataTree<GsaMember1dGoo>, DataTree<GsaMember2dGoo>>(mem1ds, mem2ds);
        }

        public static Tuple<Tuple<List<int>, List<string>>, Tuple<List<List<int>>, List<List<string>>>,
            Tuple<List<List<int>>, List<List<string>>>, List<int>> topology_detangler(string gsa_topology)
        {
            List<string> top = new List<string>();
            List<string> voids = new List<string>();
            List<string> lines = new List<string>();
            List<string> points = new List<string>();
            //string gsa_topology = "7 8 9 a 10 11 7 V(12 13 a 14 15) L(16 a 18 17) 94 P 20 P(19 21 22) L(23 24) 84";
            gsa_topology = gsa_topology.ToUpper();
            char[] spearator = { '(', ')' };

            String[] strlist = gsa_topology.Split(spearator);
            List<String> topos = new List<String>(strlist);
            List<String> topolist = new List<String>();

            // first split out anything in brackets and put them into lists for V, L or P
            // also remove those lines so that they dont appear twice in the end
            for (int i = 0; i < topos.Count(); i++)
            {
                if (topos[i].Length > 1)
                {
                    if (topos[i].Substring(topos[i].Length - 1, 1) == "V")
                    {
                        topos[i] = topos[i].Substring(0, topos[i].Length - 1);
                        voids.Add(topos[i + 1]);
                        topos.RemoveAt(i + 1);
                        continue;
                    }
                }

                if (topos[i].Length > 1)
                {
                    if (topos[i].Substring(topos[i].Length - 1, 1) == "L")
                    {
                        topos[i] = topos[i].Substring(0, topos[i].Length - 1);
                        lines.Add(topos[i + 1]);
                        topos.RemoveAt(i + 1);
                        continue;
                    }
                }

                if (topos[i].Length > 1)
                {
                    if (topos[i].Substring(topos[i].Length - 1, 1) == "P")
                    {
                        topos[i] = topos[i].Substring(0, topos[i].Length - 1);
                        points.Add(topos[i + 1]);
                        topos.RemoveAt(i + 1);
                        continue;
                    }
                }
            }

            // then split list with whitespace
            List<String> topolos = new List<String>();
            for (int i = 0; i < topos.Count(); i++)
            {
                List<String> temptopos = new List<String>(topos[i].Split(' '));
                topolos.AddRange(temptopos);
            }

            // also split list of points by whitespace as they go to single list
            List<String> pts = new List<String>();
            for (int i = 0; i < points.Count(); i++)
            {
                List<String> temppts = new List<String>(points[i].Split(' '));
                pts.AddRange(temppts);
            }

            // voids and lines needs to be made into list of lists
            List<List<int>> void_topo = new List<List<int>>();
            List<List<String>> void_topoType = new List<List<String>>();
            for (int i = 0; i < voids.Count(); i++)
            {
                List<String> tempvoids = new List<String>(voids[i].Split(' '));
                List<int> tmpvds = new List<int>();
                List<String> tmpType = new List<String>();
                for (int j = 0; j < tempvoids.Count(); j++)
                {
                    if (tempvoids[j] == "A")
                    {
                        tmpType.Add("A");
                        tempvoids.RemoveAt(j);
                    }
                    else
                        tmpType.Add(" ");
                    int tpt = Int32.Parse(tempvoids[j]);
                    tmpvds.Add(tpt);
                }
                void_topo.Add(tmpvds);
                void_topoType.Add(tmpType);
            }
            List<List<int>> incLines_topo = new List<List<int>>();
            List<List<String>> inclLines_topoType = new List<List<String>>();
            for (int i = 0; i < lines.Count(); i++)
            {
                List<String> templines = new List<String>(lines[i].Split(' '));
                List<int> tmplns = new List<int>();
                List<String> tmpType = new List<String>();
                for (int j = 0; j < templines.Count(); j++)
                {
                    if (templines[j] == "A")
                    {
                        tmpType.Add("A");
                        templines.RemoveAt(j);
                    }
                    else
                        tmpType.Add(" ");
                    int tpt = Int32.Parse(templines[j]);
                    tmplns.Add(tpt);
                }
                incLines_topo.Add(tmplns);
                inclLines_topoType.Add(tmpType);
            }

            // then remove empty entries
            for (int i = 0; i < topolos.Count(); i++)
            {
                if (topolos[i] == null)
                {
                    topolos.RemoveAt(i);
                    i = i - 1;
                    continue;
                }
                if (topolos[i].Length < 1)
                {
                    topolos.RemoveAt(i);
                    i = i - 1;
                    continue;
                }
            }

            // Find any single inclusion points not in brackets
            for (int i = 0; i < topolos.Count(); i++)
            {
                if (topolos[i] == "P")
                {
                    pts.Add(topolos[i + 1]);
                    topolos.RemoveAt(i + 1);
                    topolos.RemoveAt(i);
                    i = i - 1;
                    continue;
                }
                if (topolos[i].Length < 1)
                {
                    topolos.RemoveAt(i);
                    i = i - 1;
                    continue;
                }
            }
            List<int> inclpoint = new List<int>();
            for (int i = 0; i < pts.Count(); i++)
            {
                int tpt = Int32.Parse(pts[i]);
                inclpoint.Add(tpt);
            }

            // write out topology type (A) to list
            List<int> topoint = new List<int>();
            List<String> topoType = new List<String>();
            for (int i = 0; i < topolos.Count(); i++)
            {
                if (topolos[i] == "A")
                {
                    topoType.Add("A");
                    continue;
                }
                topoType.Add(" ");
                int tpt = Int32.Parse(topolos[i]);
                topoint.Add(tpt);
            }
            Tuple<List<int>, List<string>> topoTuple = new Tuple<List<int>, List<string>>(topoint, topoType);
            Tuple<List<List<int>>, List<List<string>>> voidTuple = new Tuple<List<List<int>>, List<List<string>>>(void_topo, void_topoType);
            Tuple<List<List<int>>, List<List<string>>> lineTuple = new Tuple<List<List<int>>, List<List<string>>>(incLines_topo, inclLines_topoType);
            
            return new Tuple<Tuple<List<int>, List<string>>, Tuple<List<List<int>>, List<List<string>>>,
            Tuple<List<List<int>>, List<List<string>>>, List<int>>(topoTuple, voidTuple, lineTuple, inclpoint);
        }
    }
}