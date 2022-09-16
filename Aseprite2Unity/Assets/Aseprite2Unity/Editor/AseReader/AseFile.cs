using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Aseprite2Unity.Editor
{
    public class AseFile
    {
        public AseFrameTagsChunk TagsChunk { get; }

        public string GroupToImport { get; }
        public string TagsAlsoImport { get; }
        public AseHeader Header { get; }
        public List<AseFrame> Frames { get; }
        public List<AseLayerChunk> Layers { get; }

        public AseFile(AseReader reader, string groupToImport, string tagsAlsoImport)
        {
            GroupToImport = groupToImport;
            TagsAlsoImport = tagsAlsoImport;

            Header = new AseHeader(reader);
            Frames = Enumerable.Repeat<AseFrame>(null, Header.NumFrames).ToList();
            for (int i = 0; i < Header.NumFrames; i++)
            {
                Frames[i] = new AseFrame(this, reader, i);
            }

            var firstFrame = Frames.FirstOrDefault();
            Layers = firstFrame.Chunks.OfType<AseLayerChunk>().ToList();

            // get the tags from the chunks in the first frame.
            TagsChunk = firstFrame.Chunks.OfType<AseFrameTagsChunk>().FirstOrDefault();


            if (GroupToImport != null)
            {
                //TODO: do this while iterating and not after.
                firstFrame.Chunks.RemoveAll(l => l.ChunkType == ChunkType.Layer);

                var parents = new List<AseLayerChunk>();
                AseLayerChunk previous = null;
                var i = 0;
                foreach (var layer in Layers)
                {
                    if (previous != null)
                    {
                        if (layer.ChildLevel > previous.ChildLevel)
                        {
                            parents.Add(previous);
                        }
                        if (layer.ChildLevel < previous.ChildLevel)
                        {
                            parents.RemoveAt(parents.Count - 1);
                        }
                    }

                    layer.Group = parents.FirstOrDefault();
                    layer.Parent = parents.LastOrDefault();
                    layer.Index = i;

                    previous = layer;
                    i++;
                }

                var matchingLayers = Layers
                                        .Where(l => l.Group != null && l.Group.Name.StartsWith(this.GroupToImport));

                // Remove the tags that don't match the group
                foreach (var frameTagEntry in TagsChunk.Entries.ToArray())
                {
                    if (!frameTagEntry.Name.StartsWith(this.GroupToImport) && !frameTagEntry.Name.StartsWith(this.TagsAlsoImport))
                        TagsChunk.Entries.Remove(frameTagEntry);
                }

                // Remove each Frame that does not match any group tags
                foreach (var frame in Frames.ToArray())
                {
                    var matchingTags = TagsChunk.Entries;
                    if (matchingTags.Count() > 0 && !matchingTags.Any(mt => frame.Index >= mt.FromFrame && frame.Index <= mt.ToFrame))
                    {
                        Frames.Remove(frame);
                    }
                    else
                    {
                        // Remove each cell that is not of a matching layer
                        var cells = frame.Chunks.OfType<AseCelChunk>();
                        foreach (var cell in cells.ToArray())
                        {
                            if (!matchingLayers.Any(l => l.Index == cell.LayerIndex))
                            {
                                frame.Chunks.Remove(cell);
                            }
                        }
                    }
                }
            }
        }

        public void VisitContents(IAseVisitor visitor)
        {
            visitor.BeginFileVisit(this);

            foreach (var frame in Frames)
            {
                visitor.BeginFrameVisit(frame);

                foreach (var chunk in frame.Chunks)
                {
                    chunk.Visit(visitor);
                }

                visitor.EndFrameVisit(frame);
            }

            visitor.EndFileVisit(this);
        }

    }
}
