import {useSettings} from '../Context/SettingsContext';
import ContentCard from '../Components/VideoCard';
import { useSelectedVideo } from "../Context/SelectedVideoContext";
import {Content} from "../Models/types";

export default function Videos() {
  const {state} = useSettings();
  const {setSelectedVideo} = useSelectedVideo();

  const handlePlay = (video: Content) => {
    setSelectedVideo(video);
  };

  return (
    <div className="p-5 space-y-6 rounded-lg">
      <h1 className="text-3xl font-bold mb-4">Clips</h1>
      {state.content.length > 0 ? (
        <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 xl:grid-cols-4 2xl:grid-cols-5 gap-4">
          {state.content
            .filter((video) => video.type.toLocaleLowerCase() === 'clip')
            .map((video, index) => (
            <ContentCard
              key={index}
              content={video}
              onClick={handlePlay}
              type={'clip'}
            />
          ))}
        </div>
      ) : (
        <p className="text-center">No videos available.</p>
      )}
    </div>
  );
}
