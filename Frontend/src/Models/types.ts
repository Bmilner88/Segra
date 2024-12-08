export interface Content {
	type: 'Video' | 'Clip';
	title: string;
	game: string;
	fileName: string;
	filePath: string;
	fileSize: string;
	duration: string;
	createdAt: string;
}

export interface State {
	recording?: Recording;
	hasLoadedObs: boolean;
	isCreatingClip: boolean;
	content: Content[];
	inputDevices: AudioDevice[];
	outputDevices: AudioDevice[];
}

export interface Recording {
	startTime: Date;
	endTime: Date;
	game: string;
}

export interface Settings {
	theme: 'recaps' | 'rich' | 'dark' | 'night' | 'dracula' | 'black' | 'luxury' | 'forest' | 'halloween' | 'coffee' | 'dim' | 'sunset';
	resolution: '720p' | '1080p' | '1440p' | '4K';
	frameRate: number;
	rateControl: string;
	crfValue: number;
	cqLevel: number;
	bitrate: number;
	encoder: 'gpu' | 'cpu';
	codec: 'h264' | 'h265';
	storageLimit: number;
	contentFolder: string;
	inputDevice: string;
	outputDevice: string;
	keyframeInterval: number;
	preset: 'fast' | 'medium' | 'slow';
	profile: 'baseline' | 'main' | 'high';
	state: State;
}

export const initialState: State = {
	recording: undefined,
	hasLoadedObs: true,
	isCreatingClip: false,
	content: [],
	inputDevices: [],
	outputDevices: [],
};

export const initialSettings: Settings = {
	theme: 'recaps',
	resolution: '720p',
	frameRate: 30,
	rateControl: 'CBR',
	crfValue: 23,
	cqLevel: 20,
	bitrate: 10,
	encoder: 'gpu',
	codec: 'h264',
	storageLimit: 100,
	contentFolder: '',
	inputDevice: '',
	outputDevice: '',
	keyframeInterval: 2,
	preset: 'medium',
	profile: 'main',
	state: initialState,
};

export interface AudioDevice {
	id: string;
	name: string;
}