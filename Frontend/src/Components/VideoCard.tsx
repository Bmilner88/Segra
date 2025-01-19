import {useSettings} from '../Context/SettingsContext';
import {Content} from '../Models/types';
import {sendMessageToBackend} from '../Utils/MessageUtils'

type VideoType = 'video' | 'clip';

interface VideoCardProps {
	content?: Content; // Optional for skeleton cards
	type: VideoType;
	onClick?: (video: Content) => void; // Click handler for the entire card
	isLoading?: boolean; // Indicates if this is a loading (skeleton) card
}

export default function ContentCard({content, type, onClick, isLoading}: VideoCardProps) {
	const {contentFolder} = useSettings();

	if (isLoading) {
		// Render a skeleton card
		return (
			<div className="card card-compact bg-base-300 text-gray-300 shadow-xl w-full border border-secondary">
				<figure className="relative aspect-w-16 aspect-h-9">
					{/* Thumbnail Skeleton */}
					<div className="skeleton w-full h-0 relative rounded-none" style={{paddingTop: '56.25%'}}></div>
					<span className="absolute bottom-2 right-2 bg-opacity-75 text-white text-xs rounded skeleton w-full" style={{aspectRatio: '16/9', visibility: 'hidden'}}></span>

				</figure>
				<div className="card-body ">
					{/* Title Skeleton */}
					<div className="skeleton bg-base-300 h-5 w-3/4 mb-2 mt-1"></div>
					{/* Metadata Skeleton */}
					<div className="skeleton h-4 w-4/6"></div>
				</div>
			</div>
		);
	}

	const getThumbnailPath = (): string => {
		const contentFileName = `${contentFolder}/.thumbnails/${type}s/${content?.fileName}.png`;
		return `http://localhost:2222/api/thumbnail?input=${encodeURIComponent(contentFileName)}`; // API route for thumbnails
	};

	const formatDuration = (duration: string): string => {
		try {
			const time = duration.split('.')[0]; // Remove fractional seconds
			const [hours, minutes, seconds] = time.split(':').map(Number);

			const totalMinutes = (hours || 0) * 60 + (minutes || 0); // Convert hours to minutes
			return `${totalMinutes.toString().padStart(2, '0')}:${seconds.toString().padStart(2, '0')}`;
		} catch {
			return '00:00'; // Fallback for invalid duration
		}
	};

	const thumbnailPath = getThumbnailPath();
	const formattedDuration = formatDuration(content!.duration);

	const handleDelete = () => {
		const parameters: any = {
			FileName: content!.fileName,
			ContentType: type,
		};

		sendMessageToBackend('DeleteContent', parameters)
	};

	return (
		<div
			className="card card-compact bg-base-300 text-gray-300 shadow-xl w-full border border-secondary border-opacity-75 cursor-pointer"
			onClick={() => onClick?.(content!)}
		>
			<figure className="relative aspect-ratio[16/9]">
				<img
					src={thumbnailPath}
					alt={"thumbnail"}
					className="w-full h-full object-cover"
					loading="lazy"
					width={1600}
					height={900}
				/>
				<span className="absolute bottom-2 right-2 bg-black bg-opacity-75 text-white text-xs px-2 py-1 rounded">
					{formattedDuration}
				</span>
			</figure>

			<div className="card-body">
				<h2 className="card-title truncate">{content!.title || (content!.game || 'Untitled')}</h2>
				<p className="text-sm text-gray-200 flex items-center">
					<span>{content!.fileSize} &bull; {new Date(content!.createdAt).toLocaleDateString()}</span>
				</p>

				<div className="dropdown dropdown-end absolute right-3 rounded-md" onClick={(e) => e.stopPropagation()}>
					<div tabIndex={0} role="button btn-ghost">
						<svg fill="#e5e7eb" height={26} width={24} version="1.1" id="Capa_1" xmlns="http://www.w3.org/2000/svg" viewBox="0 0 32.055 32.055"><g id="SVGRepo_bgCarrier" strokeWidth="0"></g><g id="SVGRepo_tracerCarrier" strokeLinecap="round" strokeLinejoin="round"></g><g id="SVGRepo_iconCarrier"> <g> <path d="M3.968,12.061C1.775,12.061,0,13.835,0,16.027c0,2.192,1.773,3.967,3.968,3.967c2.189,0,3.966-1.772,3.966-3.967 C7.934,13.835,6.157,12.061,3.968,12.061z M16.233,12.061c-2.188,0-3.968,1.773-3.968,3.965c0,2.192,1.778,3.967,3.968,3.967 s3.97-1.772,3.97-3.967C20.201,13.835,18.423,12.061,16.233,12.061z M28.09,12.061c-2.192,0-3.969,1.774-3.969,3.967 c0,2.19,1.774,3.965,3.969,3.965c2.188,0,3.965-1.772,3.965-3.965S30.278,12.061,28.09,12.061z"></path> </g> </g></svg>
					</div>
					<ul tabIndex={0} className="dropdown-content menu bg-base-100 rounded-box z-[999] w-52 p-2 shadow">
						<li>
							<a
								onClick={() => {
									// I don't know why it doesn't hide by itself?
									(document.activeElement as HTMLElement).blur();

									handleDelete();
								}}
							>
								Delete
							</a>
						</li>
					</ul>
				</div>
			</div>
		</div>
	);
}