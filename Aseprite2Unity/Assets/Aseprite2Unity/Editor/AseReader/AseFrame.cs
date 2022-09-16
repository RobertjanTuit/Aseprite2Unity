using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Aseprite2Unity.Editor
{
    public class AseFrame
    {
        public int Index { get; }

        public AseFile AseFile { get; }

        public uint NumBytesInFrame { get; }
        public ushort MagicNumber { get; }
        public uint NumChunks { get; }
        public ushort FrameDurationMs { get; }

        public List<AseChunk> Chunks { get; }
        public UnityEngine.Sprite Sprite { get; internal set; }

        public AseFrame(AseFile file, AseReader reader, int index)
        {
            Index = index;
            AseFile = file;

            NumBytesInFrame = reader.ReadDWORD();
            MagicNumber = reader.ReadWORD();
            NumChunks = reader.ReadWORD();
            FrameDurationMs = reader.ReadWORD();

            // Ingore next two bytes
            reader.ReadBYTEs(2);

            // Later versions of Aseprite may overwrite our number of chunks
            var nchunks = reader.ReadDWORD();
            if (NumChunks == 0xFFFF && NumChunks < nchunks)
            {
                NumChunks = nchunks;
            }

            // Read in old and new chunks
            Chunks = Enumerable.Repeat<AseChunk>(null, (int)NumChunks).ToList();
            for (int i = 0; i < NumChunks; i++)
            {
                Chunks[i] = ReadChunk(reader);
            }
            
            /*
            Chunks = Chunks
                        .Where((c => c.ChunkType != ChunkType.Layer || (c as AseLayerChunk).Flags.HasFlag(LayerChunkFlags.Visible)))
                        .ToList();

            NumChunks = (uint)Chunks.Count();
            */

            Debug.Assert(MagicNumber == 0xF1FA);
        }

        private AseChunk ReadChunk(AseReader reader)
        {
            uint size = reader.ReadDWORD();
            ChunkType type = (ChunkType)reader.ReadWORD();
            return ChunkFactory.ReadChunk(this, type, (int)(size - 6), reader);
        }
    }
}
